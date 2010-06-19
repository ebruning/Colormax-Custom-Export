using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Kofax.Eclipse.Base;

namespace ColormaxCustomExport
{
    public partial class ColormaxCustomExport : Form
    {
        private readonly List<IPageOutputConverter> m_PageConverters = new List<IPageOutputConverter>();
        private readonly List<IDocumentOutputConverter> m_DocConverters = new List<IDocumentOutputConverter>();
        private Guid m_CurrentFileTypeId;
        
        public string Destination
        {
            get { return input_Destination.Text; }
        }

        public Guid FileTypeId
        {
            get { return m_CurrentFileTypeId; }
        }

        public ReleaseMode WorkingMode
        {
            get
            {
                if (option_Single.Checked)
                    return ReleaseMode.SinglePage;
                if (option_Multi.Checked)
                    return ReleaseMode.MultiPage;
                
                throw new Exception("Settings inconsistency!");
            }
        }

        public string IndexField
        {
            get
            {
                if (cbIndexValue.SelectedItem == null)
                    return string.Empty;
                
                return cbIndexValue.SelectedItem.ToString();
            }
        }

        public Int32 PaddingValue
        {
            get
            {
                return Int32.Parse(txtPadding.Text);
            }
        }

        public bool DeleteFirstPage
        {
            get { return cbDeleteFirstPage.Checked; }
        }

        public ColormaxCustomExport(
            IEnumerable<IExporter> exporters, 
            string destination,
            Guid fileTypeId,
            ReleaseMode workingMode,
            IIndexField[] indexFields,
            string indexField,
            int padding,
            bool deleteFirstPage)
        {
            InitializeComponent();

            this.Text = "Colormax Custom Exporter Setup - " +
                        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            /// Reflecting current settings onto UI controls
            input_Destination.Text = destination;
            m_CurrentFileTypeId = fileTypeId;

            /// An exporter can support both interfaces
            foreach (IExporter exporter in exporters)
            {
                if (exporter is IPageOutputConverter)
                    m_PageConverters.Add(exporter as IPageOutputConverter);
                if (exporter is IDocumentOutputConverter)
                    m_DocConverters.Add(exporter as IDocumentOutputConverter);
            }

            option_Single.Checked = workingMode == ReleaseMode.SinglePage;
            //option_Multi.Checked = workingMode == ReleaseMode.MultiPage;

            //set single page option
            option_Single.Checked = true;

            cbIndexValue.Items.Clear();

            foreach (IIndexField field in indexFields)
                cbIndexValue.Items.Add(field.Label);

            for (int count = 0; count < indexFields.Length; count++ )
            {
                if (indexField.Equals(cbIndexValue.Items[count]))
                    cbIndexValue.SelectedIndex = count;
            }
            txtPadding.Text = padding.ToString();
            cbDeleteFirstPage.Checked = deleteFirstPage;
        }

        private void combo_FileType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combo_FileType.SelectedIndex < 0)
                return;

            /// Upon changing, the selected converter's ID and customizability will be reflected in the UI

            if (option_Single.Checked && combo_FileType.SelectedIndex < m_PageConverters.Count)
            {
                button_SetupFileType.Enabled = m_PageConverters[combo_FileType.SelectedIndex].IsCustomizable;
                m_CurrentFileTypeId = m_PageConverters[combo_FileType.SelectedIndex].Id;
                UpdateExampleTxt();
            }

            if (option_Multi.Checked && combo_FileType.SelectedIndex < m_DocConverters.Count)
            {
                button_SetupFileType.Enabled = m_DocConverters[combo_FileType.SelectedIndex].IsCustomizable;
                m_CurrentFileTypeId = m_DocConverters[combo_FileType.SelectedIndex].Id;
                UpdateExampleTxt();
            }
       }

        private void button_SetupFileType_Click(object sender, EventArgs e)
        {
            if (combo_FileType.SelectedIndex < 0)
                return;

            /// The associated button is only enabled if the selected file converter supports customization.

            if (option_Single.Checked && combo_FileType.SelectedIndex < m_PageConverters.Count)
                m_PageConverters[combo_FileType.SelectedIndex].Setup(new Dictionary<string, string>());

            if (option_Multi.Checked && combo_FileType.SelectedIndex < m_DocConverters.Count)
                m_DocConverters[combo_FileType.SelectedIndex].Setup(new Dictionary<string, string>());
        }

        private void button_BrowseDestination_Click(object sender, EventArgs e)
        {
            destinationBrowserDialog.SelectedPath = input_Destination.Text;
            if (destinationBrowserDialog.ShowDialog(this) == DialogResult.OK)
                input_Destination.Text = destinationBrowserDialog.SelectedPath;
        }

        private void button_OK_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(input_Destination.Text))
            {
                MessageBox.Show(
                    "Please specify a release destination",
                    "Missing information",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            if (combo_FileType.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select an output file type", 
                    "Missing information", 
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation);
                return;
            }
            DialogResult = DialogResult.OK;
        }

        private void option_SingleMulti_CheckedChanged(object sender, EventArgs e)
        {
            /// Upon switching the release mode, the list of available file types for that particular mode
            /// will be updated. The dialog will attempt to keep the current converter by holding its ID, since
            /// a converter can support both document and page output conversion.

            combo_FileType.Items.Clear();

            if (option_Single.Checked)
                foreach (IPageOutputConverter pageConverter in m_PageConverters)
                {
                    combo_FileType.Items.Add(string.Format("{0} - {1}", pageConverter.DefaultExtension, pageConverter.Name));
                    if (pageConverter.Id == m_CurrentFileTypeId)
                        combo_FileType.SelectedIndex = m_PageConverters.IndexOf(pageConverter);
                }
            
            if (option_Multi.Checked)
                foreach (IDocumentOutputConverter docConverter in m_DocConverters)
                {
                    combo_FileType.Items.Add(string.Format("{0} - {1}", docConverter.DefaultExtension, docConverter.Name));
                    if (docConverter.Id == m_CurrentFileTypeId)
                        combo_FileType.SelectedIndex = m_DocConverters.IndexOf(docConverter);
                }
        }

        private void UpdateExampleTxt()
        {
            if (combo_FileType.SelectedIndex < 0)
                return;

            int[] pageNumber = {1, 2, 3, 4, 5, 6};
            string indexValue = "<" + cbIndexValue.SelectedItem + ">";
            string extension = m_PageConverters[combo_FileType.SelectedIndex].DefaultExtension;

            txtSampleFile.Clear();

            foreach (Int32 number in pageNumber)
            {
                txtSampleFile.Text += string.Format("{0}{1}.{2}", number.ToString("D" + txtPadding.Text), indexValue, extension) + Environment.NewLine;
            }
        }

        private void cbIndexValue_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateExampleTxt();
        }

        private void txtPadding_TextChanged(object sender, EventArgs e)
        {
            UpdateExampleTxt();
        }
    }
}