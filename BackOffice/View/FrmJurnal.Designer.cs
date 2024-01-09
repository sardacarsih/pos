
namespace BackOffice.View
{
    partial class FrmJurnal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmJurnal));
            DevExpress.XtraGrid.GridFormatRule gridFormatRule1 = new DevExpress.XtraGrid.GridFormatRule();
            DevExpress.XtraGrid.GridFormatRule gridFormatRule2 = new DevExpress.XtraGrid.GridFormatRule();
            DevExpress.XtraGrid.GridFormatRule gridFormatRule3 = new DevExpress.XtraGrid.GridFormatRule();
            imageCollection1 = new DevExpress.Utils.ImageCollection(components);
            TABJurnal = new DevExpress.XtraTab.XtraTabControl();
            xtraTabPage2 = new DevExpress.XtraTab.XtraTabPage();
            cmballperiode = new ComboBox();
            groupControl1 = new DevExpress.XtraEditors.GroupControl();
            cefilterlengkap = new DevExpress.XtraEditors.CheckEdit();
            lblrecordbulan = new DevExpress.XtraEditors.LabelControl();
            defiltertanggal = new DevExpress.XtraEditors.DateEdit();
            txtfilterketerangan = new DevExpress.XtraEditors.TextEdit();
            txtfilterjumlah = new DevExpress.XtraEditors.TextEdit();
            txtfilterkode = new DevExpress.XtraEditors.TextEdit();
            txtfilternojurnal = new DevExpress.XtraEditors.TextEdit();
            sbfilterexport = new DevExpress.XtraEditors.SimpleButton();
            sbfilterclear = new DevExpress.XtraEditors.SimpleButton();
            simpleButton3 = new DevExpress.XtraEditors.SimpleButton();
            simpleButton4 = new DevExpress.XtraEditors.SimpleButton();
            GCHeader = new DevExpress.XtraGrid.GridControl();
            GVHeader = new DevExpress.XtraGrid.Views.Grid.GridView();
            GCDetails = new DevExpress.XtraGrid.GridControl();
            GVDetail = new DevExpress.XtraGrid.Views.Grid.GridView();
            HIDREFF = new DevExpress.XtraGrid.Columns.GridColumn();
            NOJURNAL = new DevExpress.XtraGrid.Columns.GridColumn();
            TANGGAL = new DevExpress.XtraGrid.Columns.GridColumn();
            BARIS = new DevExpress.XtraGrid.Columns.GridColumn();
            KODEACC = new DevExpress.XtraGrid.Columns.GridColumn();
            REKENINGD = new DevExpress.XtraGrid.Columns.GridColumn();
            DEBETD = new DevExpress.XtraGrid.Columns.GridColumn();
            KREDITD = new DevExpress.XtraGrid.Columns.GridColumn();
            KETERANGAND = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn10 = new DevExpress.XtraGrid.Columns.GridColumn();
            gridColumn11 = new DevExpress.XtraGrid.Columns.GridColumn();
            labelControl3 = new DevExpress.XtraEditors.LabelControl();
            behaviorManager1 = new DevExpress.Utils.Behaviors.BehaviorManager(components);
            dragDropEvents1 = new DevExpress.Utils.DragDrop.DragDropEvents(components);
            ((System.ComponentModel.ISupportInitialize)imageCollection1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)TABJurnal).BeginInit();
            TABJurnal.SuspendLayout();
            xtraTabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)groupControl1).BeginInit();
            groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)cefilterlengkap.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)defiltertanggal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)defiltertanggal.Properties.CalendarTimeProperties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtfilterketerangan.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtfilterjumlah.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtfilterkode.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)txtfilternojurnal.Properties).BeginInit();
            ((System.ComponentModel.ISupportInitialize)GCHeader).BeginInit();
            ((System.ComponentModel.ISupportInitialize)GVHeader).BeginInit();
            ((System.ComponentModel.ISupportInitialize)GCDetails).BeginInit();
            ((System.ComponentModel.ISupportInitialize)GVDetail).BeginInit();
            ((System.ComponentModel.ISupportInitialize)behaviorManager1).BeginInit();
            SuspendLayout();
            // 
            // imageCollection1
            // 
            imageCollection1.ImageStream = (DevExpress.Utils.ImageCollectionStreamer)resources.GetObject("imageCollection1.ImageStream");
            // 
            // TABJurnal
            // 
            TABJurnal.Dock = DockStyle.Fill;
            TABJurnal.Location = new Point(0, 0);
            TABJurnal.Margin = new Padding(637, 747, 637, 747);
            TABJurnal.Name = "TABJurnal";
            TABJurnal.SelectedTabPage = xtraTabPage2;
            TABJurnal.Size = new Size(1384, 717);
            TABJurnal.TabIndex = 0;
            TABJurnal.TabPages.AddRange(new DevExpress.XtraTab.XtraTabPage[] { xtraTabPage2 });
            // 
            // xtraTabPage2
            // 
            xtraTabPage2.Controls.Add(cmballperiode);
            xtraTabPage2.Controls.Add(groupControl1);
            xtraTabPage2.Controls.Add(simpleButton3);
            xtraTabPage2.Controls.Add(simpleButton4);
            xtraTabPage2.Controls.Add(GCHeader);
            xtraTabPage2.Controls.Add(GCDetails);
            xtraTabPage2.Controls.Add(labelControl3);
            xtraTabPage2.Margin = new Padding(10, 11, 10, 11);
            xtraTabPage2.Name = "xtraTabPage2";
            xtraTabPage2.Size = new Size(1382, 692);
            xtraTabPage2.Text = "&Daftar Jurnal F8";
            // 
            // cmballperiode
            // 
            cmballperiode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmballperiode.FormattingEnabled = true;
            cmballperiode.Location = new Point(99, 14);
            cmballperiode.Margin = new Padding(2, 3, 2, 3);
            cmballperiode.MaxDropDownItems = 12;
            cmballperiode.Name = "cmballperiode";
            cmballperiode.Size = new Size(94, 21);
            cmballperiode.TabIndex = 12;
            cmballperiode.SelectedIndexChanged += cmballperiode_SelectedIndexChanged;
            // 
            // groupControl1
            // 
            groupControl1.Controls.Add(cefilterlengkap);
            groupControl1.Controls.Add(lblrecordbulan);
            groupControl1.Controls.Add(defiltertanggal);
            groupControl1.Controls.Add(txtfilterketerangan);
            groupControl1.Controls.Add(txtfilterjumlah);
            groupControl1.Controls.Add(txtfilterkode);
            groupControl1.Controls.Add(txtfilternojurnal);
            groupControl1.Controls.Add(sbfilterexport);
            groupControl1.Controls.Add(sbfilterclear);
            groupControl1.Location = new Point(274, 15);
            groupControl1.Margin = new Padding(2, 3, 2, 3);
            groupControl1.Name = "groupControl1";
            groupControl1.Size = new Size(1052, 78);
            groupControl1.TabIndex = 10;
            groupControl1.Text = "Filter By";
            // 
            // cefilterlengkap
            // 
            cefilterlengkap.Location = new Point(791, 43);
            cefilterlengkap.Name = "cefilterlengkap";
            cefilterlengkap.Properties.Caption = "Lengkap";
            cefilterlengkap.Size = new Size(69, 20);
            cefilterlengkap.TabIndex = 8;
            // 
            // lblrecordbulan
            // 
            lblrecordbulan.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblrecordbulan.Location = new Point(928, 50);
            lblrecordbulan.Name = "lblrecordbulan";
            lblrecordbulan.Size = new Size(68, 13);
            lblrecordbulan.TabIndex = 11;
            lblrecordbulan.Text = "Filter Record :";
            // 
            // defiltertanggal
            // 
            defiltertanggal.EditValue = null;
            defiltertanggal.Location = new Point(137, 29);
            defiltertanggal.Name = "defiltertanggal";
            defiltertanggal.Properties.AdvancedModeOptions.Label = "Tanggal";
            defiltertanggal.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] { new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo) });
            defiltertanggal.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            defiltertanggal.Size = new Size(100, 34);
            defiltertanggal.TabIndex = 6;
            defiltertanggal.EditValueChanged += defiltertanggal_EditValueChanged;
            // 
            // txtfilterketerangan
            // 
            txtfilterketerangan.Location = new Point(499, 29);
            txtfilterketerangan.Margin = new Padding(2, 3, 2, 3);
            txtfilterketerangan.Name = "txtfilterketerangan";
            txtfilterketerangan.Properties.AdvancedModeOptions.Label = "Keterangan";
            txtfilterketerangan.Properties.EditValueChangedDelay = 100;
            txtfilterketerangan.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtfilterketerangan.Size = new Size(235, 34);
            txtfilterketerangan.TabIndex = 3;
            txtfilterketerangan.KeyDown += txtfilterketerangan_KeyDown;
            // 
            // txtfilterjumlah
            // 
            txtfilterjumlah.EditValue = "0";
            txtfilterjumlah.Location = new Point(361, 29);
            txtfilterjumlah.Margin = new Padding(2, 3, 2, 3);
            txtfilterjumlah.Name = "txtfilterjumlah";
            txtfilterjumlah.Properties.AdvancedModeOptions.Label = "Jumlah =";
            txtfilterjumlah.Properties.EditValueChangedDelay = 100;
            txtfilterjumlah.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            txtfilterjumlah.Properties.MaskSettings.Set("MaskManagerSignature", "allowNull=False");
            txtfilterjumlah.Properties.MaskSettings.Set("mask", "###,###,###,##0.00;");
            txtfilterjumlah.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtfilterjumlah.Size = new Size(134, 34);
            txtfilterjumlah.TabIndex = 2;
            txtfilterjumlah.KeyDown += txtfilterjumlah_KeyDown;
            // 
            // txtfilterkode
            // 
            txtfilterkode.Location = new Point(242, 29);
            txtfilterkode.Margin = new Padding(2, 3, 2, 3);
            txtfilterkode.Name = "txtfilterkode";
            txtfilterkode.Properties.AdvancedModeOptions.Label = "Kode";
            txtfilterkode.Properties.EditValueChangedDelay = 100;
            txtfilterkode.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtfilterkode.Size = new Size(115, 34);
            txtfilterkode.TabIndex = 1;
            txtfilterkode.KeyDown += txtfilterkode_KeyDown;
            // 
            // txtfilternojurnal
            // 
            txtfilternojurnal.Location = new Point(7, 29);
            txtfilternojurnal.Margin = new Padding(2, 3, 2, 3);
            txtfilternojurnal.Name = "txtfilternojurnal";
            txtfilternojurnal.Properties.AdvancedModeOptions.Label = "No. Jurnal";
            txtfilternojurnal.Properties.EditValueChangedDelay = 100;
            txtfilternojurnal.Properties.UseAdvancedMode = DevExpress.Utils.DefaultBoolean.True;
            txtfilternojurnal.Size = new Size(125, 34);
            txtfilternojurnal.TabIndex = 0;
            txtfilternojurnal.KeyDown += txtfilternojurnal_KeyDown;
            // 
            // sbfilterexport
            // 
            sbfilterexport.Appearance.Options.UseTextOptions = true;
            sbfilterexport.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            sbfilterexport.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            sbfilterexport.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.RightCenter;
            sbfilterexport.Location = new Point(865, 29);
            sbfilterexport.Margin = new Padding(2, 3, 2, 3);
            sbfilterexport.Name = "sbfilterexport";
            sbfilterexport.Size = new Size(48, 34);
            sbfilterexport.TabIndex = 5;
            sbfilterexport.ToolTip = "Export Jurnal";
            sbfilterexport.Click += Sbfilterexport_Click;
            // 
            // sbfilterclear
            // 
            sbfilterclear.Appearance.Options.UseTextOptions = true;
            sbfilterclear.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            sbfilterclear.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            sbfilterclear.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.RightCenter;
            sbfilterclear.Location = new Point(738, 29);
            sbfilterclear.Margin = new Padding(2, 3, 2, 3);
            sbfilterclear.Name = "sbfilterclear";
            sbfilterclear.Size = new Size(48, 34);
            sbfilterclear.TabIndex = 5;
            sbfilterclear.ToolTip = "Clear Filter";
            sbfilterclear.Click += Sbfilterclear_Click;
            // 
            // simpleButton3
            // 
            simpleButton3.Appearance.Options.UseTextOptions = true;
            simpleButton3.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            simpleButton3.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            simpleButton3.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.RightCenter;
            simpleButton3.Location = new Point(197, 13);
            simpleButton3.Margin = new Padding(2, 3, 2, 3);
            simpleButton3.Name = "simpleButton3";
            simpleButton3.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            simpleButton3.Size = new Size(31, 25);
            simpleButton3.TabIndex = 2;
            simpleButton3.Click += simpleButton3_Click;
            // 
            // simpleButton4
            // 
            simpleButton4.Appearance.Options.UseTextOptions = true;
            simpleButton4.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            simpleButton4.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            simpleButton4.ImageOptions.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.RightCenter;
            simpleButton4.Location = new Point(65, 15);
            simpleButton4.Margin = new Padding(2, 3, 2, 3);
            simpleButton4.Name = "simpleButton4";
            simpleButton4.PaintStyle = DevExpress.XtraEditors.Controls.PaintStyles.Light;
            simpleButton4.Size = new Size(30, 20);
            simpleButton4.TabIndex = 1;
            simpleButton4.Click += simpleButton4_Click;
            // 
            // GCHeader
            // 
            GCHeader.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            GCHeader.EmbeddedNavigator.Margin = new Padding(2, 3, 2, 3);
            GCHeader.Location = new Point(16, 100);
            GCHeader.MainView = GVHeader;
            GCHeader.Margin = new Padding(2, 3, 2, 3);
            GCHeader.Name = "GCHeader";
            GCHeader.Size = new Size(251, 570);
            GCHeader.TabIndex = 1;
            GCHeader.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { GVHeader });
            GCHeader.Click += GCHeader_Click;
            GCHeader.KeyUp += GCHeader_KeyUp;
            // 
            // GVHeader
            // 
            GVHeader.Appearance.FocusedCell.BackColor = Color.FromArgb(128, 128, 255);
            GVHeader.Appearance.FocusedCell.Options.UseBackColor = true;
            GVHeader.Appearance.FocusedRow.BackColor = Color.FromArgb(128, 128, 255);
            GVHeader.Appearance.FocusedRow.Options.UseBackColor = true;
            GVHeader.DetailHeight = 331;
            GVHeader.GridControl = GCHeader;
            GVHeader.Name = "GVHeader";
            GVHeader.OptionsBehavior.Editable = false;
            GVHeader.OptionsFind.FindFilterColumns = "";
            GVHeader.OptionsFind.ShowFindButton = false;
            GVHeader.OptionsSelection.CheckBoxSelectorColumnWidth = 30;
            GVHeader.OptionsSelection.MultiSelect = true;
            GVHeader.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;
            GVHeader.OptionsView.ShowGroupPanel = false;
            GVHeader.GotFocus += GVHeader_GotFocus;
            // 
            // GCDetails
            // 
            GCDetails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            GCDetails.EmbeddedNavigator.Margin = new Padding(7386, 9683, 7386, 9683);
            GCDetails.Location = new Point(274, 100);
            GCDetails.MainView = GVDetail;
            GCDetails.Margin = new Padding(7386, 9683, 7386, 9683);
            GCDetails.Name = "GCDetails";
            GCDetails.Size = new Size(1097, 570);
            GCDetails.TabIndex = 5;
            GCDetails.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] { GVDetail });
            // 
            // GVDetail
            // 
            GVDetail.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] { HIDREFF, NOJURNAL, TANGGAL, BARIS, KODEACC, REKENINGD, DEBETD, KREDITD, KETERANGAND, gridColumn10, gridColumn11 });
            GVDetail.DetailHeight = 18947;
            gridFormatRule1.Name = "Format0";
            gridFormatRule1.Rule = null;
            gridFormatRule2.Name = "Format1";
            gridFormatRule2.Rule = null;
            gridFormatRule3.Name = "Format2";
            gridFormatRule3.Rule = null;
            GVDetail.FormatRules.Add(gridFormatRule1);
            GVDetail.FormatRules.Add(gridFormatRule2);
            GVDetail.FormatRules.Add(gridFormatRule3);
            GVDetail.GridControl = GCDetails;
            GVDetail.GroupSummary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] { new DevExpress.XtraGrid.GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "DEBET", DEBETD, "{0:n2}"), new DevExpress.XtraGrid.GridGroupSummaryItem(DevExpress.Data.SummaryItemType.Sum, "KREDIT", KREDITD, "{0:n2}") });
            GVDetail.HorzScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Always;
            GVDetail.Name = "GVDetail";
            GVDetail.OptionsBehavior.ReadOnly = true;
            GVDetail.OptionsClipboard.CopyColumnHeaders = DevExpress.Utils.DefaultBoolean.False;
            GVDetail.OptionsCustomization.AllowColumnMoving = false;
            GVDetail.OptionsCustomization.AllowSort = false;
            GVDetail.OptionsFilter.AllowFilterEditor = false;
            GVDetail.OptionsFilter.InHeaderSearchMode = DevExpress.XtraGrid.Views.Grid.GridInHeaderSearchMode.Disabled;
            GVDetail.OptionsFind.FindNullPrompt = "";
            GVDetail.OptionsView.BestFitMode = DevExpress.XtraGrid.Views.Grid.GridBestFitMode.Fast;
            GVDetail.OptionsView.ShowFooter = true;
            GVDetail.OptionsView.ShowGroupPanel = false;
            GVDetail.SortInfo.AddRange(new DevExpress.XtraGrid.Columns.GridColumnSortInfo[] { new DevExpress.XtraGrid.Columns.GridColumnSortInfo(BARIS, DevExpress.Data.ColumnSortOrder.Ascending) });
            GVDetail.VertScrollVisibility = DevExpress.XtraGrid.Views.Base.ScrollVisibility.Always;
            GVDetail.KeyDown += GVDetail_KeyDown;
            // 
            // HIDREFF
            // 
            HIDREFF.Caption = "HIDREFF";
            HIDREFF.FieldName = "HIDREFF";
            HIDREFF.MinWidth = 17;
            HIDREFF.Name = "HIDREFF";
            HIDREFF.Width = 66;
            // 
            // NOJURNAL
            // 
            NOJURNAL.Caption = "NOJURNAL";
            NOJURNAL.FieldName = "NOJURNAL";
            NOJURNAL.MinWidth = 17;
            NOJURNAL.Name = "NOJURNAL";
            NOJURNAL.Width = 66;
            // 
            // TANGGAL
            // 
            TANGGAL.Caption = "TANGGAL";
            TANGGAL.FieldName = "TANGGAL";
            TANGGAL.MinWidth = 17;
            TANGGAL.Name = "TANGGAL";
            TANGGAL.Width = 66;
            // 
            // BARIS
            // 
            BARIS.Caption = "NO";
            BARIS.FieldName = "BARIS";
            BARIS.MinWidth = 17;
            BARIS.Name = "BARIS";
            BARIS.OptionsFilter.AllowFilter = false;
            BARIS.Visible = true;
            BARIS.VisibleIndex = 0;
            BARIS.Width = 40;
            // 
            // KODEACC
            // 
            KODEACC.Caption = "KODE";
            KODEACC.FieldName = "Kode";
            KODEACC.MinWidth = 17;
            KODEACC.Name = "KODEACC";
            KODEACC.OptionsFilter.AutoFilterCondition = DevExpress.XtraGrid.Columns.AutoFilterCondition.BeginsWith;
            KODEACC.Visible = true;
            KODEACC.VisibleIndex = 1;
            KODEACC.Width = 170;
            // 
            // REKENINGD
            // 
            REKENINGD.Caption = "REKENING";
            REKENINGD.FieldName = "Rekening";
            REKENINGD.MinWidth = 17;
            REKENINGD.Name = "REKENINGD";
            REKENINGD.Visible = true;
            REKENINGD.VisibleIndex = 2;
            REKENINGD.Width = 170;
            // 
            // DEBETD
            // 
            DEBETD.Caption = "DEBET";
            DEBETD.DisplayFormat.FormatString = "N2";
            DEBETD.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            DEBETD.FieldName = "Debet";
            DEBETD.MinWidth = 17;
            DEBETD.Name = "DEBETD";
            DEBETD.OptionsFilter.AutoFilterCondition = DevExpress.XtraGrid.Columns.AutoFilterCondition.GreaterOrEqual;
            DEBETD.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] { new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Debet", "{0:N2}") });
            DEBETD.Visible = true;
            DEBETD.VisibleIndex = 3;
            DEBETD.Width = 170;
            // 
            // KREDITD
            // 
            KREDITD.Caption = "KREDIT";
            KREDITD.DisplayFormat.FormatString = "N2";
            KREDITD.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            KREDITD.FieldName = "Kredit";
            KREDITD.MinWidth = 17;
            KREDITD.Name = "KREDITD";
            KREDITD.OptionsFilter.AutoFilterCondition = DevExpress.XtraGrid.Columns.AutoFilterCondition.GreaterOrEqual;
            KREDITD.Summary.AddRange(new DevExpress.XtraGrid.GridSummaryItem[] { new DevExpress.XtraGrid.GridColumnSummaryItem(DevExpress.Data.SummaryItemType.Sum, "Kredit", "{0:N2}") });
            KREDITD.Visible = true;
            KREDITD.VisibleIndex = 4;
            KREDITD.Width = 170;
            // 
            // KETERANGAND
            // 
            KETERANGAND.Caption = "KETERANGAN";
            KETERANGAND.FieldName = "Keterangan";
            KETERANGAND.MinWidth = 17;
            KETERANGAND.Name = "KETERANGAND";
            KETERANGAND.Visible = true;
            KETERANGAND.VisibleIndex = 5;
            KETERANGAND.Width = 178;
            // 
            // gridColumn10
            // 
            gridColumn10.Caption = "POSTED";
            gridColumn10.FieldName = "POSTED";
            gridColumn10.MinWidth = 17;
            gridColumn10.Name = "gridColumn10";
            gridColumn10.Width = 66;
            // 
            // gridColumn11
            // 
            gridColumn11.Caption = "PERIODE";
            gridColumn11.FieldName = "PERIODE";
            gridColumn11.MinWidth = 17;
            gridColumn11.Name = "gridColumn11";
            gridColumn11.Width = 66;
            // 
            // labelControl3
            // 
            labelControl3.Location = new Point(19, 15);
            labelControl3.Margin = new Padding(637, 747, 637, 747);
            labelControl3.Name = "labelControl3";
            labelControl3.Size = new Size(36, 13);
            labelControl3.TabIndex = 0;
            labelControl3.Text = "Periode";
            // 
            // FrmJurnal
            // 
            Appearance.Options.UseFont = true;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1384, 717);
            Controls.Add(TABJurnal);
            Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            KeyPreview = true;
            Margin = new Padding(3, 6, 3, 6);
            Name = "FrmJurnal";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Jurnal";
            FormClosed += FrmJurnal_FormClosed;
            Load += FrmJurnal_Load;
            ((System.ComponentModel.ISupportInitialize)imageCollection1).EndInit();
            ((System.ComponentModel.ISupportInitialize)TABJurnal).EndInit();
            TABJurnal.ResumeLayout(false);
            xtraTabPage2.ResumeLayout(false);
            xtraTabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)groupControl1).EndInit();
            groupControl1.ResumeLayout(false);
            groupControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)cefilterlengkap.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)defiltertanggal.Properties.CalendarTimeProperties).EndInit();
            ((System.ComponentModel.ISupportInitialize)defiltertanggal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtfilterketerangan.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtfilterjumlah.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtfilterkode.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)txtfilternojurnal.Properties).EndInit();
            ((System.ComponentModel.ISupportInitialize)GCHeader).EndInit();
            ((System.ComponentModel.ISupportInitialize)GVHeader).EndInit();
            ((System.ComponentModel.ISupportInitialize)GCDetails).EndInit();
            ((System.ComponentModel.ISupportInitialize)GVDetail).EndInit();
            ((System.ComponentModel.ISupportInitialize)behaviorManager1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DevExpress.XtraTab.XtraTabControl TABJurnal;
        private DevExpress.XtraTab.XtraTabPage xtraTabPage2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraGrid.GridControl GCDetails;
        private DevExpress.XtraGrid.Views.Grid.GridView GVDetail;
        private DevExpress.Utils.ImageCollection imageCollection1;
        private DevExpress.XtraGrid.Columns.GridColumn HIDREFF;
        private DevExpress.XtraGrid.Columns.GridColumn NOJURNAL;
        private DevExpress.XtraGrid.Columns.GridColumn TANGGAL;
        private DevExpress.XtraGrid.Columns.GridColumn BARIS;
        private DevExpress.XtraGrid.Columns.GridColumn KODEACC;
        private DevExpress.XtraGrid.Columns.GridColumn REKENINGD;
        private DevExpress.XtraGrid.Columns.GridColumn DEBETD;
        private DevExpress.XtraGrid.Columns.GridColumn KREDITD;
        private DevExpress.XtraGrid.Columns.GridColumn KETERANGAND;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn10;
        private DevExpress.XtraGrid.Columns.GridColumn gridColumn11;
        private DevExpress.XtraGrid.GridControl GCHeader;
        private DevExpress.XtraGrid.Views.Grid.GridView GVHeader;
        private DevExpress.XtraEditors.SimpleButton simpleButton3;
        private DevExpress.XtraEditors.SimpleButton simpleButton4;
        private DevExpress.Utils.Behaviors.BehaviorManager behaviorManager1;
        private DevExpress.Utils.DragDrop.DragDropEvents dragDropEvents1;
        private DevExpress.XtraEditors.LabelControl lblrecordbulan;
        private DevExpress.XtraEditors.GroupControl groupControl1;
        private DevExpress.XtraEditors.CheckEdit cefilterlengkap;
        private DevExpress.XtraEditors.DateEdit defiltertanggal;
        private DevExpress.XtraEditors.TextEdit txtfilterketerangan;
        private DevExpress.XtraEditors.TextEdit txtfilterjumlah;
        private DevExpress.XtraEditors.TextEdit txtfilterkode;
        private DevExpress.XtraEditors.TextEdit txtfilternojurnal;
        private DevExpress.XtraEditors.SimpleButton sbfilterexport;
        private DevExpress.XtraEditors.SimpleButton sbfilterclear;
        private System.Windows.Forms.ComboBox cmballperiode;
    }
}