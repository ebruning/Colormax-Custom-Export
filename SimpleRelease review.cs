using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Kofax.Eclipse.Base;

namespace Kofax.Eclipse.SimpleRelease
{
    public class SimpleRelease : IReleaseScript
    {
        #region Standard interface to inspect the script's properties and basic settings
        /// <summary>
        /// This GUID is used to uniquely identify the script from the application. 
        /// Generate one from Visual Studio when you first create the script and 
        /// keep it for the rest of its life.
        /// </summary>
        public Guid Id
        {
            get { return new Guid("{20D7C4C2-ECA8-4157-BB75-7FDD46120286}"); }
        }

        /// <summary>
        /// This name will shows up in the list of available release scripts 
        /// in the application's UI. It should be localized.
        /// </summary>
        public string Name
        {
            get { return "Simple Release"; }
        }

        /// <summary>
        /// This description will shows up whenever the application decide to 
        /// briefly explain the script's purpose to the user. Should also be localized.
        /// </summary>
        public string Description
        {
            get { return "A very simple release script. Provided as a educational sample for Express release SDK development"; }
        }

        /// <summary>
        /// The script will release batches using the mode it remembered from a previous setup
        /// </summary>
        public ReleaseMode WorkingMode
        {
            get { return m_WorkingMode; }
        }

        /// <summary>
        /// This simple script will happily process batches in both single and multi-page release modes.
        /// </summary>
        public bool IsSupported(ReleaseMode mode)
        {
            return true;
        } 
        #endregion

        #region Script settings - Will be remembered across sessions
        /// <summary>
        /// The script will release batches in this mode. Can be configured by the setup dialog and remembered across sessions
        /// </summary>
        private ReleaseMode m_WorkingMode = ReleaseMode.SinglePage;

        /// <summary>
        /// The destination to place the released pages. Under this destination, 
        /// a folder structure of "[BatchName]\[DocNumber]"will be created
        /// </summary>
        private string m_Destination = string.Empty;

        /// <summary>
        /// Id of the user-selected file type converter to convert the released documents/pages
        /// </summary>
        private Guid m_FileTypeId = Guid.Empty; 
        #endregion

        #region Instance settings - Valid only throughout the running session
        /// <summary>
        /// Reference to the actively employed converter to pass the pages through
        /// </summary>
        private IPageOutputConverter m_PageConverter;

        /// <summary>
        /// Reference to the actively employed converter to pass the documents through
        /// </summary>
        private IDocumentOutputConverter m_DocConverter;

        /// <summary>
        /// Point to the folder to where the whole released batch will go
        /// </summary>
        private string m_BatchFolder;

        /// <summary>
        /// Point to the folder to where the pages in the currently release document will go
        /// </summary>
        private string m_DocFolder; 
        #endregion

        #region Handlers to be called during an actual release process
        /// <summary>
        /// This method will be called first when the release start. The application will pass latest information 
        /// from the running instance to the script through given parameters. The script should do its final check
        /// for proper release conditions, throwing exceptions when things go wrong.
        /// </summary>
        public object StartRelease(IList<IExporter> exporters, IIndexField[] indexFields, IDictionary<string, string> releaseData)
        {
            if (string.IsNullOrEmpty(m_Destination))
                throw new Exception("Please specify a release destination");

            m_DocConverter = null;
            m_PageConverter = null;

            foreach (IExporter exporter in exporters)
                if (exporter.Id == m_FileTypeId)
                {
                    if (m_WorkingMode == ReleaseMode.SinglePage)
                        m_PageConverter = exporter as IPageOutputConverter;
                    else
                        m_DocConverter = exporter as IDocumentOutputConverter;
                }

            /// When both of them can't be found, either the user haven't setup or the chosen converter has disappeared.
            /// The script can declare that the release cannot continue or proceed with a default set of settings.
            if (m_PageConverter == null && m_DocConverter == null)
                throw new Exception("Please select an output file type");

            /// The application will keep any object returned from this function and pass it back to the script 
            /// in the EndRelease call. This is usually intended to facilitate cleanup.
            return null;
        }

        /// <summary>
        /// This method will be called after the batch had been readied by the application 
        /// but before any document/page is sent to the script. The scripts usually perform 
        /// preparations to release the batch based on the current settings.
        /// </summary>
        public object StartBatch(IBatch batch)
        {
            m_BatchFolder = Path.Combine(m_Destination, batch.Name);

            bool batchFolderCreated = !Directory.Exists(m_BatchFolder);
            if (batchFolderCreated)
                Directory.CreateDirectory(m_BatchFolder);

            /// Again, the application will keep any object returned from this function and pass it back to the script 
            /// in the EndBatch call. This is usually intended to facilitate cleanup.
            return batchFolderCreated;
        }

        /// <summary>
        /// In multipage release mode, this method will be called for every documents in the batch.
        /// This script will simply pass them to the chosen document output converter to produce
        /// the expected output files in the released batch folder
        /// </summary>
        public void Release(IDocument doc)
        {
            string outputFileName = Path.Combine(m_BatchFolder, doc.Number.ToString());
            m_DocConverter.Convert(doc, Path.ChangeExtension(outputFileName, m_DocConverter.DefaultExtension));
        }

        /// <summary>
        /// For every document, this method will be called after it had been readied by the application 
        /// but before any page is sent to the script. The scripts usually perform preparations to release
        /// the document based on the current settings.
        /// </summary>
        public object StartDocument(IDocument doc)
        {
            m_DocFolder = Path.Combine(m_BatchFolder, doc.Number.ToString());

            bool docFolderCreated = !Directory.Exists(m_DocFolder);
            if (docFolderCreated)
                Directory.CreateDirectory(m_DocFolder);

            /// Finally, the application will keep any object returned from this function and pass it back to the script 
            /// in the EndDocument call. This is usually intended to facilitate cleanup.
            return docFolderCreated;
        }

        /// <summary>
        /// In single-page release mode, this method will be called for every pages in the batch.
        /// This script will simply pass them to the chosen page output converter to produce
        /// the expected output files in the currently released document folder
        /// </summary>
        public void Release(IPage page)
        {
            string outputFileName = Path.Combine(m_DocFolder, page.Number.ToString());
            m_PageConverter.Convert(page, Path.ChangeExtension(outputFileName, m_PageConverter.DefaultExtension));
        }

        /// <summary>
        /// For every document, this method will be called after all pages have been sent to the script. 
        /// The scripts usually perform the necessary cleanups based on current settings and the actual release conditions
        /// </summary>
        public void EndDocument(IDocument doc, object handle, ReleaseResult result)
        {
            /// The handle should always indicate whether the script created the document folder from scratch or not
            if (result != ReleaseResult.Succeeded && (bool)handle)
                Directory.Delete(m_DocFolder);
        }

        /// <summary>
        /// This method will be called after all documents have been sent to the script. 
        /// The scripts usually perform the necessary cleanups based on current settings 
        /// and the actual release conditions
        /// </summary>
        public void EndBatch(IBatch batch, object handle, ReleaseResult result)
        {
            /// The handle should always indicate whether the script created the batch folder from scratch or not
            if (result != ReleaseResult.Succeeded && (bool)handle)
                Directory.Delete(m_BatchFolder);
        }

        /// <summary>
        /// This method will be called after the everything has been sent to the script 
        /// and the batch has been closed by the application. The scripts usually perform 
        /// necessary cleanups based on current settings and the actual release conditions
        /// </summary>
        public void EndRelease(object handle, ReleaseResult result)
        {
            /// Since we don't do anything special in this simple script, 
            /// there's nothing to be cleaned up here. The handle should always be null.
        } 
        #endregion

        #region Handlers to be called during configuration requests by the user and before/after a release session
        /// <summary>
        /// Simply write whatever needs to be persisted across sessions here..
        /// </summary>
        public void SerializeSettings(Stream output)
        {
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                writer.Write(m_Destination);
                writer.Write(m_FileTypeId.ToString());
                writer.Write(m_WorkingMode.ToString());
            }
        }

        /// <summary>
        /// Simply read whatever persisted from previous sessions here..
        /// </summary>
        public void DeserializeSettings(Stream input)
        {
            using (BinaryReader reader = new BinaryReader(input))
            {
                try
                {
                    m_Destination = reader.ReadString();
                    m_FileTypeId = new Guid(reader.ReadString());
                    m_WorkingMode = (ReleaseMode)Enum.Parse(typeof(ReleaseMode), reader.ReadString());
                }
                catch
                {
                    /// If the script throw exceptions here, it wouldn't be able to recover from the application
                    /// This will be addressed in later version of the API..
                    m_Destination = string.Empty;
                    m_FileTypeId = Guid.Empty;
                    m_WorkingMode = ReleaseMode.SinglePage;
                }
            }
        }

        /// <summary>
        /// Whenever the user request to configure the script's settings, the method will be called with
        /// latest information from the application's running instance as parameters. Also, a script can
        /// define and add its own information to the data table and pass them further down to the exporters.
        /// </summary>
        public void Setup(IList<IExporter> exporters, IIndexField[] indexFields, IDictionary<string, string> releaseData)
        {
            SimpleReleaseSetup setupDialog = new SimpleReleaseSetup(exporters, m_Destination, m_FileTypeId, m_WorkingMode);
            if (setupDialog.ShowDialog() != DialogResult.OK) return;

            m_Destination = setupDialog.Destination;
            m_FileTypeId = setupDialog.FileTypeId;
            m_WorkingMode = setupDialog.WorkingMode;
        } 
        #endregion
    }
}
