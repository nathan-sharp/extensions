using System.Runtime.InteropServices;
using Inventor;
using InventorApplication = Inventor.Application;

namespace NJS.InventorMaterials;

[Guid(ClassId)]
[ProgId(ProgId)]
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.None)]
[ComDefaultInterface(typeof(ApplicationAddInServer))]
public sealed class StandardAddInServer : ApplicationAddInServer
{
    public const string ClassId = "6C5C1A92-4C4E-4B8E-9E74-7D9A7B4D2F31";
    public const string ProgId = "NJS.InventorMaterials.StandardAddInServer";

    private InventorApplication? _application;
    private ButtonDefinition? _materialStandardsButton;

    public object Automation => null!;

    public void Activate(ApplicationAddInSite addInSiteObject, bool firstTime)
    {
        _application = addInSiteObject.Application;
        _materialStandardsButton = _application.CommandManager.ControlDefinitions.AddButtonDefinition(
            "Material Standards",
            "NJS_MaterialStandards",
            CommandTypesEnum.kQueryOnlyCmdType,
            ClassId,
            "Apply a material standard from the Inventor material library to the active part.",
            "Material Standards",
            null,
            null,
            ButtonDisplayEnum.kDisplayTextInLearningMode);
        _materialStandardsButton.OnExecute += OnMaterialStandardsExecute;
        AddButtonToRibbon();
    }

    public void Deactivate()
    {
        if (_materialStandardsButton is not null)
        {
            _materialStandardsButton.OnExecute -= OnMaterialStandardsExecute;
            _materialStandardsButton = null;
        }

        _application = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public void ExecuteCommand(int commandID)
    {
    }

    private void AddButtonToRibbon()
    {
        if (_application is null || _materialStandardsButton is null)
        {
            return;
        }

        try
        {
            Ribbon ribbon = _application.UserInterfaceManager.Ribbons["Part"];
            RibbonTab toolsTab = ribbon.RibbonTabs["id_TabTools"];
            RibbonPanel panel = toolsTab.RibbonPanels.Add("NJS Materials", "NJS_Materials_Panel", ClassId);
            panel.CommandControls.AddButton(_materialStandardsButton, true, true);
        }
        catch (Exception exception)
        {
            Log($"Could not add the NJS Materials ribbon panel: {exception}");
        }
    }

    private void OnMaterialStandardsExecute(NameValueMap context)
    {
        if (_application is null)
        {
            return;
        }

        try
        {
            if (_application.ActiveDocument is not PartDocument partDocument)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Open a part document to apply a material standard.",
                    "NJS Materials | Material Standards",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            IReadOnlyList<AssetLibrary> libraries = GetMaterialLibraries();
            using var dialog = new MaterialStandardsDialog(
                MaterialStandards.All,
                standard => MaterialStandards.FindMaterial(libraries, partDocument, standard) is not null);
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK || dialog.SelectedStandard is null)
            {
                return;
            }

            MaterialStandard standard = dialog.SelectedStandard;
            MaterialAsset? material = MaterialStandards.GetOrCopyMaterial(libraries, partDocument, standard);
            if (material is null)
            {
                throw new InvalidOperationException(
                    $"The Inventor material library does not contain a material matching {standard.Designation}. " +
                    "Enable the appropriate Inventor material library in the active project and try again.");
            }

            partDocument.ActiveMaterial = (Asset)material;
            System.Windows.Forms.MessageBox.Show(
                $"Applied {standard.Designation} ({material.DisplayName}) to {partDocument.DisplayName}.",
                "NJS Materials | Material Standards",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
            Log($"Applied '{standard.Designation}' as '{material.DisplayName}' to '{partDocument.DisplayName}'.");
        }
        catch (Exception exception)
        {
            Log($"Material Standards failed: {exception}");
            System.Windows.Forms.MessageBox.Show(
                $"Could not apply the material standard.\n\n{exception.Message}",
                "NJS Materials | Material Standards",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Error);
        }
    }

    private IReadOnlyList<AssetLibrary> GetMaterialLibraries()
    {
        if (_application is null)
        {
            return Array.Empty<AssetLibrary>();
        }

        var libraries = new List<AssetLibrary>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void AddLibrary(AssetLibrary library)
        {
            if (!string.IsNullOrWhiteSpace(library.FullFileName) && paths.Add(library.FullFileName))
            {
                libraries.Add(library);
            }
        }

        AssetLibraries loadedLibraries = _application.AssetLibraries;
        for (int index = 1; index <= loadedLibraries.Count; index++)
        {
            try
            {
                AddLibrary(loadedLibraries[index]);
            }
            catch (Exception exception)
            {
                Log($"Could not inspect material library {index}: {exception.Message}");
            }
        }

        try
        {
            ProjectAssetLibraries projectLibraries = _application.DesignProjectManager.ActiveDesignProject.MaterialLibraries;
            for (int index = 1; index <= projectLibraries.Count; index++)
            {
                ProjectAssetLibrary projectLibrary = projectLibraries[index];
                if (paths.Contains(projectLibrary.LibraryFilename))
                {
                    continue;
                }

                try
                {
                    AddLibrary(_application.AssetLibraries.Open(projectLibrary.LibraryFilename));
                }
                catch (Exception exception)
                {
                    Log($"Could not open material library '{projectLibrary.LibraryFilename}': {exception.Message}");
                }
            }
        }
        catch (Exception exception)
        {
            Log($"Could not enumerate project material libraries: {exception.Message}");
        }

        return libraries;
    }

    private static void Log(string message)
    {
        try
        {
            string directory = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "NJSMaterials",
                "InventorAddIn");
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(directory, "activation.log"),
                $"{DateTimeOffset.Now:O} {message}{System.Environment.NewLine}");
        }
        catch
        {
        }
    }
}