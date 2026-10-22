using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if GSTARCAD
using GstarCAD.ApplicationServices;
using GstarCAD.DatabaseServices;
using GstarCAD.EditorInput;
using GstarCAD.Runtime;
#elif CADMATE
using CADmate.ApplicationServices;
using CADmate.DatabaseServices;
using CADmate.EditorInput;
using CADmate.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
#endif

namespace LayoutTemplatePlugin
{
    public class LayoutTemplateSettings
    {
        public string TemplateDrawingPath { get; set; } = string.Empty;
        public bool ApplyToAllLayouts { get; set; } = true;
        public List<string> LayoutNames { get; set; } = new List<string>();
    }

    public class LayoutTemplateExtension : IExtensionApplication
    {
        public void Initialize()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            doc?.Editor.WriteMessage("\nLayoutTemplatePlugin loaded. Run LAYOUTTEMPLATE.\n");
        }

        public void Terminate()
        {
        }

        [CommandMethod("LAYOUTTEMPLATE")]
        public static void LayoutTemplateCommand()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;
            var settings = PromptForSettings(ed);
            if (settings == null)
            {
                ed.WriteMessage("\nOperation cancelled.\n");
                return;
            }

            try
            {
                ApplyTemplate(doc.Database, settings);
                ed.WriteMessage("\nLayout template settings applied successfully.\n");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\nFailed to apply template settings: {ex.Message}\n");
            }
        }

        private static LayoutTemplateSettings PromptForSettings(Editor ed)
        {
            var templatePathPrompt = new PromptStringOptions("\nEnter source template drawing path: ")
            {
                AllowSpaces = true
            };

            var templatePathResult = ed.GetString(templatePathPrompt);
            if (templatePathResult.Status != PromptStatus.OK)
            {
                return null;
            }

            var templatePath = templatePathResult.StringResult.Trim();
            if (string.IsNullOrWhiteSpace(templatePath) || !File.Exists(templatePath))
            {
                ed.WriteMessage("\nTemplate drawing not found.\n");
                return null;
            }

            var applyAllPrompt = new PromptStringOptions("\nApply to all layouts? [Y/N]: ");
            var applyAllResult = ed.GetString(applyAllPrompt);
            if (applyAllResult.Status != PromptStatus.OK)
            {
                return null;
            }

            var applyToAll = applyAllResult.StringResult.Trim();
            var allLayouts = string.IsNullOrWhiteSpace(applyToAll)
                || applyToAll.Equals("Y", StringComparison.OrdinalIgnoreCase)
                || applyToAll.Equals("YES", StringComparison.OrdinalIgnoreCase);

            var selectedLayouts = new List<string>();
            if (!allLayouts)
            {
                var layoutNamePrompt = new PromptStringOptions("\nEnter layout names separated by commas: ")
                {
                    AllowSpaces = true
                };

                var layoutNameResult = ed.GetString(layoutNamePrompt);
                if (layoutNameResult.Status != PromptStatus.OK)
                {
                    return null;
                }

                selectedLayouts = layoutNameResult.StringResult
                    .Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();
            }

            return new LayoutTemplateSettings
            {
                TemplateDrawingPath = templatePath,
                ApplyToAllLayouts = allLayouts,
                LayoutNames = selectedLayouts
            };
        }

        private static void ApplyTemplate(Database targetDb, LayoutTemplateSettings settings)
        {
            using (var sourceDb = new Database(false, true))
            {
                sourceDb.ReadDwgFile(settings.TemplateDrawingPath, FileShare.Read, true, string.Empty);

                using (var targetTr = targetDb.TransactionManager.StartTransaction())
                using (var sourceTr = sourceDb.TransactionManager.StartTransaction())
                {
                    var targetLayouts = (DBDictionary)targetTr.GetObject(targetDb.LayoutDictionaryId, OpenMode.ForRead);
                    var sourceLayouts = (DBDictionary)sourceTr.GetObject(sourceDb.LayoutDictionaryId, OpenMode.ForRead);

                    foreach (DBDictionaryEntry entry in targetLayouts)
                    {
                        var layoutName = entry.Key;
                        var isSelected = settings.ApplyToAllLayouts || settings.LayoutNames.Any(x =>
                            string.Equals(x, layoutName, StringComparison.OrdinalIgnoreCase));

                        if (!isSelected)
                        {
                            continue;
                        }

                        if (!sourceLayouts.Contains(layoutName))
                        {
                            continue;
                        }

                        var targetLayoutId = (ObjectId)entry.Value;
                        var sourceLayoutId = sourceLayouts.GetAt(layoutName);

                        var targetLayout = (Layout)targetTr.GetObject(targetLayoutId, OpenMode.ForWrite);
                        var sourceLayout = (Layout)sourceTr.GetObject(sourceLayoutId, OpenMode.ForRead);

                        targetLayout.CopyFrom(sourceLayout);
                    }

                    targetTr.Commit();
                    sourceTr.Commit();
                }
            }
        }
    }
}
