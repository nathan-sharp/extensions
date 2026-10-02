using Inventor;

namespace NJS.InventorMaterials;

internal sealed record MaterialStandard(string Designation, string Description, IReadOnlyList<string> AssetNames);

internal static class MaterialStandards
{
    public static IReadOnlyList<MaterialStandard> All { get; } =
    [
        new("AISI 4140", "Chromium-molybdenum alloy steel", ["AISI 4140", "Steel, AISI 4140", "AISI 4140 Steel"]),
        new("AISI 1045", "Medium-carbon steel", ["AISI 1045", "Steel, AISI 1045", "AISI 1045 Steel"]),
        new("AISI 304", "Austenitic stainless steel", ["AISI 304", "Stainless Steel, AISI 304", "AISI 304 Stainless Steel"]),
        new("AISI 316", "Austenitic stainless steel", ["AISI 316", "Stainless Steel, AISI 316", "AISI 316 Stainless Steel"]),
        new("6061-T6", "Heat-treated aluminum alloy", ["Aluminum 6061-T6", "Aluminum, 6061-T6", "6061-T6"])
    ];

    public static MaterialAsset? FindMaterial(
        IReadOnlyList<AssetLibrary> libraries,
        PartDocument document,
        MaterialStandard standard)
    {
        AssetsEnumerator documentMaterials = document.MaterialAssets;
        for (int index = 1; index <= documentMaterials.Count; index++)
        {
            var material = (MaterialAsset)documentMaterials[index];
            if (Matches(material, standard))
            {
                return material;
            }
        }

        foreach (AssetLibrary library in libraries)
        {
            AssetsEnumerator materials = library.MaterialAssets;
            for (int index = 1; index <= materials.Count; index++)
            {
                var material = (MaterialAsset)materials[index];
                if (Matches(material, standard))
                {
                    return material;
                }
            }
        }

        return null;
    }

    public static MaterialAsset? GetOrCopyMaterial(
        IReadOnlyList<AssetLibrary> libraries,
        PartDocument document,
        MaterialStandard standard)
    {
        MaterialAsset? material = FindMaterial(libraries, document, standard);
        if (material is null || material.Parent is PartDocument)
        {
            return material;
        }

        return (MaterialAsset)material.CopyTo(document, false);
    }

    private static bool Matches(MaterialAsset material, MaterialStandard standard) =>
        standard.AssetNames.Any(assetName =>
            string.Equals(material.DisplayName, assetName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(material.Name, assetName, StringComparison.OrdinalIgnoreCase));
}

internal sealed class MaterialStandardsDialog : System.Windows.Forms.Form
{
    private readonly System.Windows.Forms.ListView _standardsList = new();

    public MaterialStandard? SelectedStandard =>
        _standardsList.SelectedItems.Count == 0
            ? null
            : (MaterialStandard)_standardsList.SelectedItems[0].Tag!;

    public MaterialStandardsDialog(IReadOnlyList<MaterialStandard> standards, Func<MaterialStandard, bool> isAvailable)
    {
        Text = "NJS Materials | Material Standards";
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new System.Drawing.Size(620, 330);
        Font = new System.Drawing.Font("Segoe UI", 9F);

        _standardsList.Dock = System.Windows.Forms.DockStyle.Fill;
        _standardsList.FullRowSelect = true;
        _standardsList.GridLines = true;
        _standardsList.HideSelection = false;
        _standardsList.MultiSelect = false;
        _standardsList.View = System.Windows.Forms.View.Details;
        _standardsList.Columns.Add("Standard", 120);
        _standardsList.Columns.Add("Description", 260);
        _standardsList.Columns.Add("Inventor library", 170);
        foreach (MaterialStandard standard in standards)
        {
            bool available = isAvailable(standard);
            var item = new System.Windows.Forms.ListViewItem(standard.Designation);
            item.SubItems.Add(standard.Description);
            item.SubItems.Add(available ? "Available" : "Not found");
            item.Tag = standard;
            item.ForeColor = available ? System.Drawing.SystemColors.WindowText : System.Drawing.Color.DimGray;
            _standardsList.Items.Add(item);
        }

        var applyButton = new System.Windows.Forms.Button { Text = "Apply", AutoSize = true, DialogResult = System.Windows.Forms.DialogResult.OK };
        var cancelButton = new System.Windows.Forms.Button { Text = "Cancel", AutoSize = true, DialogResult = System.Windows.Forms.DialogResult.Cancel };
        var buttons = new System.Windows.Forms.FlowLayoutPanel
        {
            Dock = System.Windows.Forms.DockStyle.Bottom,
            Height = 44,
            FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new System.Windows.Forms.Padding(0, 8, 8, 0)
        };
        buttons.Controls.Add(applyButton);
        buttons.Controls.Add(cancelButton);
        Controls.Add(_standardsList);
        Controls.Add(buttons);
        AcceptButton = applyButton;
        CancelButton = cancelButton;
    }
}