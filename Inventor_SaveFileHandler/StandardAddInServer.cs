using System;
using System.Runtime.InteropServices;
using Inventor;
using Microsoft.Win32;
using System.Windows.Forms;
using IO = System.IO;
using InvAddIn;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Inventor_SaveFileHandler
{
    /// <summary>
    /// This is the primary AddIn Server class that implements the ApplicationAddInServer interface
    /// that all Inventor AddIns are required to implement. The communication between Inventor and
    /// the AddIn is via the methods on this interface.
    /// </summary>
    [GuidAttribute("2c5e4482-99d6-4ca2-99c9-1213a14fcc40")]
    public class StandardAddInServer : Inventor.ApplicationAddInServer
    {

        // Inventor application object.
        private Inventor.Application m_inventorApplication;
        private Inventor.FileUIEvents m_UIevents;

        public StandardAddInServer()
        {
        }

        #region ApplicationAddInServer Members

        public void Activate(Inventor.ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            // This method is called by Inventor when it loads the addin.
            // The AddInSiteObject provides access to the Inventor Application object.
            // The FirstTime flag indicates if the addin is loaded for the first time.

            // Initialize AddIn members.
            m_inventorApplication = addInSiteObject.Application;

            m_UIevents = m_inventorApplication.FileUIEvents;
            m_UIevents.OnFileSaveAsDialog += FileUIEvents_OnFileSaveAsDialog;

            // TODO: Add ApplicationAddInServer.Activate implementation.
            // e.g. event initialization, command creation etc.
        }

        /// <summary>
        /// Handles save as event from inventor.
        /// </summary>
        /// <param name="FileTypes">Fist of file types that are supportet.</param>
        /// <param name="SaveCopyAs">Flag that indicates if save as copy is active.</param>
        /// <param name="ParentHWND">Handle of parent window.</param>
        /// <param name="FileName">Result filename</param>
        /// <param name="Context">Active context</param>
        /// <param name="HandlingCode">Result handling code.</param>
        private void FileUIEvents_OnFileSaveAsDialog(ref string[] FileTypes, bool SaveCopyAs, int ParentHWND, out string FileName, NameValueMap Context, out HandlingCodeEnum HandlingCode)
        {
            HandlingCode = HandlingCodeEnum.kEventNotHandled;
            FileName = "";

            string projectname = m_inventorApplication.DesignProjectManager.ActiveDesignProject.Name;
            string workpath = m_inventorApplication.DesignProjectManager.ActiveDesignProject.WorkspacePath;

            if (projectname == "MTL" || projectname == "Default")
            {
                return;
            }
            try
            {
                if (m_inventorApplication.ActiveDocumentType == DocumentTypeEnum.kPartDocumentObject && !SaveCopyAs)
                {
                    this.PartSaveAsHandler(workpath, projectname, ref HandlingCode, ref FileName);
                }
                else if (m_inventorApplication.ActiveDocumentType == DocumentTypeEnum.kAssemblyDocumentObject && !SaveCopyAs)
                {
                    this.AssemblySaveAsHandler(workpath, projectname, ref HandlingCode, ref FileName);
                }
                else if (m_inventorApplication.ActiveDocumentType == DocumentTypeEnum.kDrawingDocumentObject)
                {
                    this.DrawingSaveAsHandler(FileTypes, SaveCopyAs, workpath, ref HandlingCode, ref FileName);
                }
            }
            catch (Exception ex)
            {
                Routines.SerializeException(ex);
            }
        }

        /// <summary>
        /// Handles save as event for ipt parts.
        /// </summary>
        /// <param name="workpath">Path to working dir (from project settings).</param>
        /// <param name="projectname">Name of current project.</param>
        /// <param name="HandlingCode">Reference to handling code result.</param>
        /// <param name="FileName">Reference to result filename variable.</param>
        private void PartSaveAsHandler(string workpath, string projectname, ref HandlingCodeEnum HandlingCode, ref string FileName)
        {
            PropertySet designInfo = m_inventorApplication.ActiveDocument.PropertySets["Design Tracking Properties"];

            // createa a new part number dialog
            PartnumberDialog pnd = new PartnumberDialog();

            // assign dialog properties from file iproperties.
            pnd.ProjectName = projectname;
            pnd.WorkingDir = new WorkingDir(workpath);
            pnd.Vendor = designInfo["Vendor"].Value as string;
            pnd.Partnumber = designInfo["Part Number"].Value as string;
            pnd.Description = designInfo["Description"].Value as string;

            // show the dialog
            if (pnd.ShowDialog() == true)
            {
                // assign iproperties
                designInfo["Vendor"].Value = pnd.Vendor;
                designInfo["Part Number"].Value = pnd.Partnumber;
                designInfo["Description"].Value = pnd.Description;
                designInfo["Project"].Value = projectname;

                // build result filename
                string folder = string.Empty;
                string filename = string.Empty;
                switch (pnd.PartType)
                {
                    case EPartType.MakePart:
                        folder = pnd.WorkingDir.CAD;
                        filename = $"{pnd.Partnumber}_{pnd.Description}.ipt";
                        break;
                    case EPartType.CustomerPart:
                        folder = pnd.WorkingDir.Kundenteile;
                        filename = $"{pnd.Partnumber}_{pnd.Description}.ipt";
                        break;
                    case EPartType.BuyPart:
                        folder = IO.Path.Combine(pnd.WorkingDir.Kaufteile, pnd.Vendor);
                        filename = $"{pnd.Vendor}_{pnd.Partnumber}_{pnd.Description}.ipt";
                        break;
                    default:
                        return;
                }

                // assign filename and handling code.
                FileName = IO.Path.Combine(folder, filename);
                HandlingCode = HandlingCodeEnum.kEventHandled;
            }
        }

        /// <summary>
        /// Handles save as event for iam assemblys.
        /// </summary>
        /// <param name="workpath">Path to working dir (from project settings).</param>
        /// <param name="projectname">Name of current project.</param>
        /// <param name="HandlingCode">Reference to handling code result.</param>
        /// <param name="FileName">Reference to result filename variable.</param>
        private void AssemblySaveAsHandler(string workpath, string projectname, ref HandlingCodeEnum HandlingCode, ref string FileName)
        {
            PropertySet designInfo = m_inventorApplication.ActiveDocument.PropertySets["Design Tracking Properties"];

            // createa a new assembly number dialog
            AsmNumberDialog pnd = new AsmNumberDialog();

            // assign dialog properties from file iproperties.
            pnd.ProjectName = projectname;
            pnd.WorkingDir = new WorkingDir(workpath);
            pnd.Vendor = designInfo["Vendor"].Value as string;
            pnd.Partnumber = designInfo["Part Number"].Value as string;
            pnd.Description = designInfo["Description"].Value as string;

            // show the dialog
            if (pnd.ShowDialog() == true)
            {
                // assign iproperties
                designInfo["Vendor"].Value = pnd.Vendor;
                designInfo["Part Number"].Value = pnd.Partnumber;
                designInfo["Description"].Value = pnd.Description;
                designInfo["Project"].Value = projectname;


                // build result filename
                string folder = string.Empty;
                string filename = string.Empty;
                switch (pnd.PartType)
                {
                    case EPartType.MakePart:
                        folder = pnd.WorkingDir.CAD;
                        filename = $"{pnd.Partnumber}_{pnd.Description}.iam";
                        break;
                    case EPartType.BuyPart:
                        folder = IO.Path.Combine(pnd.WorkingDir.Kaufteile, pnd.Vendor);
                        filename = $"{pnd.Vendor}_{pnd.Partnumber}_{pnd.Description}.iam";
                        break;
                    default:
                        return;
                }

                // assign filename and handling code.
                FileName = IO.Path.Combine(folder, filename);
                HandlingCode = HandlingCodeEnum.kEventHandled;
            }
        }

        /// <summary>
        /// Handles save as event for drwaing documents.
        /// </summary>
        /// <param name="FileTypes">List of supportet filetypes.</param>
        /// <param name="SaveCopyAs">Flag that indicates whether the save as copy option is active.</param>
        /// <param name="workpath">Path to working dir (from project settings).</param>
        /// <param name="HandlingCode">Reference to handling code result.</param>
        /// <param name="FileName">Reference to result filename variable.</param>
        private void DrawingSaveAsHandler(string[] FileTypes, bool SaveCopyAs, string workpath, ref HandlingCodeEnum HandlingCode, ref string FileName)
        {
            // get the current 
            DrawingDocument ddoc = m_inventorApplication.ActiveDocument as DrawingDocument;
            if (ddoc == null)
            {
                return;
            }

            // create a new save file dialog
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();

            // applies all filetyes to filter of dialog
            sfd.Filter = string.Join("|", FileTypes);

            // if save as copy option 
            if (SaveCopyAs)
            {
                // navigate dialog to "..\DoKumentation\Zeichnungen"
                sfd.InitialDirectory = IO.Path.Combine(IO.Path.GetDirectoryName(workpath), "Dokumentation", "Zeichnungen");

                // filename is the same as the origin document
                sfd.FileName = $"{IO.Path.GetFileNameWithoutExtension(ddoc.FullFileName)}";

                // if pdf is available select pdf as default type
                if (FileTypes.Any(o => o.ToUpper().Contains("PDF")))
                {
                    sfd.FilterIndex = FileTypes.ToList().IndexOf(FileTypes.First(o => o.ToUpper().Contains("PDF"))) + 1;
                }
            }
            else
            {
                PropertySet designInfoOrig = null;
                
                // get the document from first view
                Document doc = null;
                if (ddoc.ReferencedDocuments.OfType<Document>().Any())
                {
                    doc = ddoc.ReferencedDocuments.OfType<Document>().First();
                    if (doc != null)
                    {
                        designInfoOrig = doc.PropertySets["Design Tracking Properties"];
                    }
                }

                // no referenced item
                if (designInfoOrig == null)
                {
                    return;
                }

                // get property set
                PropertySet designInfo = m_inventorApplication.ActiveDocument.PropertySets["Design Tracking Properties"];

                // parse part nr.
                string partNo = designInfoOrig["Part Number"].Value as string;

                // if partnr matches expression
                // PV001_B001 --> PV001_ZB001
                // PV001_T001 --> PV001_ZT001
                if (partNo != null && Regex.IsMatch(partNo, @"^(.+_)([BT]\d+)$"))
                {
                    partNo = Regex.Replace(partNo, @"^(.+_)([BT]\d+)$", "$1Z$2");
                    designInfo["Part Number"].Value = partNo;
                }
                else
                {
                    designInfo["Part Number"].Value = designInfoOrig["Part Number"].Value;
                }

                // copy descriotion from document of first view
                designInfo["Description"].Value = designInfoOrig["Description"].Value;

                // copy project of document from first view
                designInfo["Project"].Value = designInfoOrig["Project"].Value;

                // copy original filename, 
                // change _B001 --> _ZB001 
                // change _T001 --> _ZT001
                string filenameOrig = IO.Path.GetFileNameWithoutExtension(doc.FullFileName);

                if (Regex.IsMatch(filenameOrig, @"^(.+_)([BT]\d+_.+)$"))
                {
                    filenameOrig = Regex.Replace(filenameOrig, @"^(.+_)([BT]\d+_.+)$", "$1Z$2");
                }

                // set initial directory to file origin
                sfd.InitialDirectory = IO.Path.GetDirectoryName(doc.FullFileName);

                // set initial filename
                sfd.FileName = filenameOrig;
            }

            // show save as dialog
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                FileName = sfd.FileName;
                HandlingCode = HandlingCodeEnum.kEventHandled;
            }
        }

        public void Deactivate()
        {
            // This method is called by Inventor when the AddIn is unloaded.
            // The AddIn will be unloaded either manually by the user or
            // when the Inventor session is terminated

            // TODO: Add ApplicationAddInServer.Deactivate implementation
            m_UIevents.OnFileSaveAsDialog -= FileUIEvents_OnFileSaveAsDialog;

            // Release objects.
            m_UIevents = null;
            m_inventorApplication = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void ExecuteCommand(int commandID)
        {
            // Note:this method is now obsolete, you should use the 
            // ControlDefinition functionality for implementing commands.
        }

        public object Automation
        {
            // This property is provided to allow the AddIn to expose an API 
            // of its own to other programs. Typically, this  would be done by
            // implementing the AddIn's API interface in a class and returning 
            // that class object through this property.

            get
            {
                // TODO: Add ApplicationAddInServer.Automation getter implementation
                return null;
            }
        }

        #endregion

    }
}
