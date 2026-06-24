using DevExpress.LookAndFeel;
using DevExpress.XtraBars.Navigation;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using System.Runtime.CompilerServices;

namespace BackOffice.UI;

internal static class BackOfficeTheme
{
    internal static readonly Color BrandBlue = Color.FromArgb(42, 79, 126);
    internal static readonly Color BrandBlueDark = Color.FromArgb(31, 61, 101);
    internal static readonly Color AccentBlue = Color.FromArgb(42, 111, 187);
    internal static readonly Color Surface = Color.White;
    internal static readonly Color Canvas = Color.FromArgb(245, 247, 250);
    internal static readonly Color Border = Color.FromArgb(218, 224, 232);
    internal static readonly Color TextPrimary = Color.FromArgb(31, 41, 55);
    internal static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
    internal static readonly Color Danger = Color.FromArgb(190, 55, 55);
    internal static readonly Color SidebarBackground = Color.FromArgb(32, 61, 99);
    internal static readonly Color SidebarGroup = Color.FromArgb(38, 72, 115);
    internal static readonly Color SidebarHover = Color.FromArgb(51, 88, 137);
    internal static readonly Color SidebarSelected = Color.FromArgb(64, 112, 171);
    internal static readonly Color SidebarMuted = Color.FromArgb(190, 207, 226);

    private static readonly ConditionalWeakTable<Control, object?> StyledControls = new();
    private static bool initialized;

    internal static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        UserLookAndFeel.Default.SetSkinStyle("WXI");
        Application.Idle += (_, _) =>
        {
            foreach (Form form in Application.OpenForms)
            {
                if (!CanApplyTheme(form))
                {
                    continue;
                }

                Apply(form);
            }
        };
    }

    internal static void Apply(Control root)
    {
        if (!CanApplyTheme(root)
            || root.GetType().Name.Equals("Frmlogin", StringComparison.Ordinal))
        {
            return;
        }

        ApplyControl(root);
        foreach (Control child in root.Controls)
        {
            if (CanApplyTheme(child))
            {
                Apply(child);
            }
        }
    }

    internal static void StyleGrid(GridView view)
    {
        view.Appearance.HeaderPanel.BackColor = Color.FromArgb(237, 242, 247);
        view.Appearance.HeaderPanel.ForeColor = BrandBlueDark;
        view.Appearance.HeaderPanel.Font = new Font("Segoe UI Semibold", 9F);
        view.Appearance.HeaderPanel.Options.UseBackColor = true;
        view.Appearance.HeaderPanel.Options.UseForeColor = true;
        view.Appearance.HeaderPanel.Options.UseFont = true;
        view.Appearance.Row.Font = new Font("Segoe UI", 9F);
        view.Appearance.Row.ForeColor = TextPrimary;
        view.Appearance.Row.Options.UseFont = true;
        view.Appearance.Row.Options.UseForeColor = true;
        view.Appearance.EvenRow.BackColor = Color.FromArgb(249, 250, 252);
        view.Appearance.EvenRow.Options.UseBackColor = true;
        view.Appearance.FocusedRow.BackColor = Color.FromArgb(222, 235, 250);
        view.Appearance.FocusedRow.ForeColor = TextPrimary;
        view.Appearance.FocusedRow.Options.UseBackColor = true;
        view.Appearance.FocusedRow.Options.UseForeColor = true;
        view.Appearance.GroupRow.BackColor = Color.FromArgb(229, 236, 245);
        view.Appearance.GroupRow.ForeColor = BrandBlueDark;
        view.Appearance.GroupRow.Font = new Font("Segoe UI Semibold", 9F);
        view.Appearance.GroupRow.Options.UseBackColor = true;
        view.Appearance.GroupRow.Options.UseForeColor = true;
        view.Appearance.GroupRow.Options.UseFont = true;
        view.OptionsView.EnableAppearanceEvenRow = true;
        view.OptionsView.ShowIndicator = false;
        // Grids tagged "ui-fill-columns" stretch their columns to fill the grid
        // width; all others keep fixed widths (with a horizontal scrollbar).
        view.OptionsView.ColumnAutoWidth =
            Equals(view.GridControl?.Tag, "ui-fill-columns");
        view.OptionsView.RowAutoHeight = true;
        view.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.False;
        view.OptionsView.ShowVerticalLines = DevExpress.Utils.DefaultBoolean.False;
        view.OptionsSelection.EnableAppearanceHideSelection = false;
        view.RowHeight = 30;
        view.ColumnPanelRowHeight = 34;
    }

    internal static void StylePrimaryButton(SimpleButton button)
    {
        button.Tag = "ui-primary";
        button.Appearance.BackColor = AccentBlue;
        button.Appearance.ForeColor = Color.White;
        button.Appearance.Font = new Font("Segoe UI Semibold", 9F);
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
        button.Appearance.Options.UseFont = true;
        button.Height = Math.Max(button.Height, 38);
    }

    internal static void StyleSecondaryButton(SimpleButton button)
    {
        if (!Equals(button.Tag, "ui-primary"))
        {
            button.Tag = "ui-secondary";
        }
        button.Appearance.BackColor = Color.White;
        button.Appearance.ForeColor = BrandBlueDark;
        button.Appearance.Font = new Font("Segoe UI Semibold", 9F);
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
        button.Appearance.Options.UseFont = true;
        button.Height = Math.Max(button.Height, 38);
    }

    internal static void StyleSidebar(AccordionControl accordion)
    {
        accordion.Appearance.AccordionControl.BackColor = SidebarBackground;
        accordion.Appearance.AccordionControl.ForeColor = Color.White;
        accordion.Appearance.AccordionControl.Font = new Font("Segoe UI", 9.5F);
        accordion.Appearance.AccordionControl.Options.UseBackColor = true;
        accordion.Appearance.AccordionControl.Options.UseForeColor = true;
        accordion.Appearance.AccordionControl.Options.UseFont = true;

        accordion.Appearance.Group.Normal.BackColor = SidebarGroup;
        accordion.Appearance.Group.Normal.ForeColor = Color.White;
        accordion.Appearance.Group.Normal.Font = new Font("Segoe UI Semibold", 9.5F);
        accordion.Appearance.Group.Normal.Options.UseBackColor = true;
        accordion.Appearance.Group.Normal.Options.UseForeColor = true;
        accordion.Appearance.Group.Normal.Options.UseFont = true;
        accordion.Appearance.Group.Hovered.BackColor = SidebarHover;
        accordion.Appearance.Group.Hovered.ForeColor = Color.White;
        accordion.Appearance.Group.Hovered.Options.UseBackColor = true;
        accordion.Appearance.Group.Hovered.Options.UseForeColor = true;

        accordion.Appearance.Item.Normal.BackColor = SidebarBackground;
        accordion.Appearance.Item.Normal.ForeColor = SidebarMuted;
        accordion.Appearance.Item.Normal.Font = new Font("Segoe UI", 9.25F);
        accordion.Appearance.Item.Normal.Options.UseBackColor = true;
        accordion.Appearance.Item.Normal.Options.UseForeColor = true;
        accordion.Appearance.Item.Normal.Options.UseFont = true;
        accordion.Appearance.Item.Hovered.BackColor = SidebarHover;
        accordion.Appearance.Item.Hovered.ForeColor = Color.White;
        accordion.Appearance.Item.Hovered.Options.UseBackColor = true;
        accordion.Appearance.Item.Hovered.Options.UseForeColor = true;
        accordion.Appearance.Item.Pressed.BackColor = SidebarSelected;
        accordion.Appearance.Item.Pressed.ForeColor = Color.White;
        accordion.Appearance.Item.Pressed.Font = new Font("Segoe UI Semibold", 9.25F);
        accordion.Appearance.Item.Pressed.Options.UseBackColor = true;
        accordion.Appearance.Item.Pressed.Options.UseForeColor = true;
        accordion.Appearance.Item.Pressed.Options.UseFont = true;
    }

    internal static Image CreateSidebarIcon(string glyph)
    {
        const int iconSize = 20;
        var bitmap = new Bitmap(iconSize, iconSize);

        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.TextRenderingHint =
            System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var font = new Font("Segoe UI Symbol", 11F, FontStyle.Regular);
        using var brush = new SolidBrush(SidebarMuted);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString(
            glyph,
            font,
            brush,
            new RectangleF(0, 0, iconSize, iconSize),
            format);

        return bitmap;
    }

    private static void StyleDangerButton(SimpleButton button)
    {
        button.Tag = "ui-danger";
        button.Appearance.BackColor = Color.FromArgb(254, 242, 242);
        button.Appearance.ForeColor = Danger;
        button.Appearance.Font = new Font("Segoe UI Semibold", 9F);
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
        button.Appearance.Options.UseFont = true;
        button.Height = Math.Max(button.Height, 38);
    }

    private static void ApplyControl(Control control)
    {
        if (!CanApplyTheme(control))
        {
            return;
        }

        if (StyledControls.TryGetValue(control, out _))
        {
            return;
        }

        StyledControls.Add(control, new object());
        control.ControlAdded += (_, e) => Apply(e.Control);

        if (control is Form form)
        {
            form.Font = new Font("Segoe UI", 9F);
            form.BackColor = Canvas;
        }
        else if (control is AccordionControl accordion)
        {
            StyleSidebar(accordion);
        }
        else if (control is GridControl grid)
        {
            grid.BackColor = Surface;
            if (grid.MainView is GridView view)
            {
                StyleGrid(view);
            }
        }
        else if (control is GroupControl group)
        {
            group.Appearance.BackColor = Surface;
            group.Appearance.Options.UseBackColor = true;
            group.AppearanceCaption.ForeColor = BrandBlueDark;
            group.AppearanceCaption.Font = new Font("Segoe UI Semibold", 10F);
            group.AppearanceCaption.Options.UseForeColor = true;
            group.AppearanceCaption.Options.UseFont = true;
        }
        else if (control is SimpleButton button)
        {
            string caption = button.Text.Trim();
            if (Equals(button.Tag, "ui-primary") || IsPrimaryAction(caption))
            {
                StylePrimaryButton(button);
            }
            else if (Equals(button.Tag, "ui-danger") || IsDangerAction(caption))
            {
                StyleDangerButton(button);
            }
            else
            {
                StyleSecondaryButton(button);
            }
        }
        else if (control is BaseEdit editor)
        {
            editor.Font = new Font("Segoe UI", 9F);
            editor.MinimumSize = new Size(0, 28);
        }
        else if (control is LabelControl label)
        {
            label.Appearance.ForeColor = Equals(label.Tag, "ui-title")
                ? TextPrimary
                : TextMuted;
            label.Appearance.Options.UseForeColor = true;
        }
        else if (control is PanelControl panel)
        {
            panel.Appearance.BackColor = Surface;
            panel.Appearance.Options.UseBackColor = true;
            panel.BorderStyle = Equals(panel.Tag, "ui-card")
                ? DevExpress.XtraEditors.Controls.BorderStyles.Simple
                : DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
        }
    }

    private static bool CanApplyTheme(Control control)
    {
        return !control.IsDisposed
            && !control.Disposing
            && !control.InvokeRequired
            && !IsDevExpressInfrastructureForm(control);
    }

    private static bool IsDevExpressInfrastructureForm(Control control)
    {
        if (control is not Form)
        {
            return false;
        }

        for (Type? type = control.GetType(); type is not null; type = type.BaseType)
        {
            string? typeNamespace = type.Namespace;
            if (type.Name.Equals("DemoWaitForm", StringComparison.Ordinal)
                || typeNamespace?.StartsWith(
                    "DevExpress.XtraWaitForm",
                    StringComparison.Ordinal) == true
                || typeNamespace?.StartsWith(
                    "DevExpress.XtraSplashScreen",
                    StringComparison.Ordinal) == true)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPrimaryAction(string caption)
    {
        string[] actions = ["Simpan", "Tambah", "Proses", "Bayar", "Terapkan", "Masuk", "Login"];
        return actions.Any(action =>
            caption.Equals(action, StringComparison.OrdinalIgnoreCase)
            || caption.StartsWith($"{action} ", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDangerAction(string caption)
    {
        return caption.Equals("Hapus", StringComparison.OrdinalIgnoreCase)
            || caption.StartsWith("Hapus ", StringComparison.OrdinalIgnoreCase);
    }
}
