// <copyright file="StandardAddInServer.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using Inventor;
    using IO = System.IO;
    using System.Collections.Generic;

    /// <summary>
    /// This is the primary AddIn Server class that implements the ApplicationAddInServer interface
    /// that all Inventor AddIns are required to implement. The communication between Inventor and
    /// the AddIn is via the methods on this interface.
    /// </summary>
    [GuidAttribute("2c5e4482-99d6-4ca2-99c9-1213a14fcc40")]
    public class StandardAddInServer : Inventor.ApplicationAddInServer
    {
        /// <summary>
        /// Event handler for UI events.
        /// </summary>
        private FileUIEvents uIevents;

        /// <summary>
        /// Initializes a new instance of the <see cref="StandardAddInServer"/> class.
        /// </summary>
        public StandardAddInServer()
        {
            StandardAddInServer.This = this;
        }

        /// <summary>
        /// Gets a static reference to the loaded instance.
        /// </summary>
        public static StandardAddInServer This { get; private set; }

        /// <summary>
        /// Gets the Inventor application object.
        /// </summary>
        public Inventor.Application InventorApplication { get; private set; }

        /// <summary>
        /// Gets the property is provided to allow the AddIn to expose an API
        /// of its own to other programs. Typically, this  would be done by
        /// implementing the AddIn's API interface in a class and returning
        /// that class object through this property.
        /// </summary>
        public object Automation
        {
            get
            {
                // TODO: Add ApplicationAddInServer.Automation getter implementation
                return null;
            }
        }

        /// <inheritdoc/>
        public void Activate(Inventor.ApplicationAddInSite addInSiteObject, bool firstTime)
        {
            // This method is called by Inventor when it loads the addin.
            // The AddInSiteObject provides access to the Inventor Application object.
            // The FirstTime flag indicates if the addin is loaded for the first time.

            // Initialize AddIn members.
            this.InventorApplication = addInSiteObject.Application;

            this.uIevents = this.InventorApplication.FileUIEvents;
            this.uIevents.OnFileSaveAsDialog += this.FileUIEvents_OnFileSaveAsDialog;
            this.InventorApplication.ApplicationEvents.OnSaveDocument += this.RefreshPhysical;

            // TODO: Add ApplicationAddInServer.Activate implementation.
            // e.g. event initialization, command creation etc.
        }

        /// <inheritdoc/>
        public void Deactivate()
        {
            // This method is called by Inventor when the AddIn is unloaded.
            // The AddIn will be unloaded either manually by the user or
            // when the Inventor session is terminated

            // TODO: Add ApplicationAddInServer.Deactivate implementation
            this.uIevents.OnFileSaveAsDialog -= this.FileUIEvents_OnFileSaveAsDialog;

            // Release objects.
            this.uIevents = null;
            this.InventorApplication = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <inheritdoc/>
        public void ExecuteCommand(int commandID)
        {
            // Note:this method is now obsolete, you should use the
            // ControlDefinition functionality for implementing commands.
        }

        /// <summary>
        /// Handles save as event from inventor.
        /// </summary>
        /// <param name="fileTypes">Fist of file types that are supported.</param>
        /// <param name="saveCopyAs">Flag that indicates if save as copy is active.</param>
        /// <param name="parentHWND">Handle of parent window.</param>
        /// <param name="fileName">Result filename</param>
        /// <param name="context">Active context</param>
        /// <param name="handlingCode">Result handling code.</param>
        private void FileUIEvents_OnFileSaveAsDialog(
            ref string[] fileTypes,
            bool saveCopyAs,
            int parentHWND,
            out string fileName,
            NameValueMap context,
            out HandlingCodeEnum handlingCode)
        {
            handlingCode = HandlingCodeEnum.kEventNotHandled;
            fileName = string.Empty;

            Dictionary<string, object> contextClear = new Dictionary<string, object>();

            for (int i = 0; i < context.Count; i++)
            {
                contextClear.Add(context.Name[i + 1], context.Value[context.Name[i + 1]]);
            }

            string projectname = this.InventorApplication.DesignProjectManager.ActiveDesignProject.Name;
            string workpath = this.InventorApplication.DesignProjectManager.ActiveDesignProject.WorkspacePath;

            if (string.IsNullOrWhiteSpace(projectname) || projectname == "MTL" || projectname == "Default" || string.IsNullOrWhiteSpace(workpath))
            {
                return;
            }

            if (contextClear.ContainsKey("TopLevelDocument"))
            {
                Document doc = contextClear["TopLevelDocument"] as Document;

                try
                {
                    if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject && !saveCopyAs)
                    {
                        this.PartSaveAsHandler(ref doc, workpath, projectname, ref handlingCode, ref fileName);
                    }
                    else if (doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject && !saveCopyAs)
                    {
                        this.AssemblySaveAsHandler(ref doc, workpath, projectname, ref handlingCode, ref fileName);
                    }
                    else if (doc.DocumentType == DocumentTypeEnum.kDrawingDocumentObject)
                    {
                        this.DrawingSaveAsHandler(ref doc, fileTypes, saveCopyAs, workpath, ref handlingCode, ref fileName);
                    }
                }
                catch (Exception ex)
                {
                    Routines.SerializeException(ex);
                }
            }
        }

        /// <summary>
        /// Handles save as event for IPT parts.
        /// </summary>
        /// <param name="doc">document to be saved.</param>
        /// <param name="workpath">Path to working directory (from project settings).</param>
        /// <param name="projectname">Name of current project.</param>
        /// <param name="handlingCode">Reference to handling code result.</param>
        /// <param name="fileName">Reference to result filename variable.</param>
        private void PartSaveAsHandler(ref Document doc,  string workpath, string projectname, ref HandlingCodeEnum handlingCode, ref string fileName)
        {
            PropertySet designInfo = doc.PropertySets["Design Tracking Properties"];

            // creates a new part number dialog
            PartnumberDialog pnd = new PartnumberDialog();

            // assign dialog properties from file i-properties.
            pnd.ProjectName = projectname;
            pnd.WorkingDir = new WorkingDir(workpath);
            pnd.Vendor = designInfo["Vendor"].Value as string;
            pnd.Partnumber = designInfo["Part Number"].Value as string;
            pnd.Description = designInfo["Description"].Value as string;

            // show the dialog
            if (pnd.ShowDialog() == true)
            {
                // assign i-properties
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

                        PropertySet invCustomPropertySet = this.InventorApplication.ActiveDocument.PropertySets["Inventor User Defined Properties"];

                        // save halbzeug properties
                        if (invCustomPropertySet.OfType<Property>().Any(o => o.Name == "Halbzeug"))
                        {
                            invCustomPropertySet["Halbzeug"].Value = pnd.OuterDimensions;
                        }
                        else
                        {
                            invCustomPropertySet.Add(pnd.OuterDimensions, "Halbzeug");
                        }

                        if (invCustomPropertySet.OfType<Property>().Any(o => o.Name == "HalbzeugNeuBerechnen"))
                        {
                            invCustomPropertySet["HalbzeugNeuBerechnen"].Value = pnd.ReElevateDimensionOnSave;
                        }
                        else
                        {
                            invCustomPropertySet.Add(pnd.ReElevateDimensionOnSave, "HalbzeugNeuBerechnen");
                        }

                        if (invCustomPropertySet.OfType<Property>().Any(o => o.Name == "Drehteil"))
                        {
                            invCustomPropertySet["Drehteil"].Value = pnd.IsRotatePart;
                        }
                        else
                        {
                            invCustomPropertySet.Add(pnd.IsRotatePart, "Drehteil");
                        }

                        (doc as PartDocument).ComponentDefinition.BOMStructure = BOMStructureEnum.kNormalBOMStructure;
                        break;
                    case EPartType.CustomerPart:
                        folder = pnd.WorkingDir.Kundenteile;
                        filename = $"{pnd.Partnumber}_{pnd.Description}.ipt";
                        (doc as PartDocument).ComponentDefinition.BOMStructure = BOMStructureEnum.kReferenceBOMStructure;
                        break;
                    case EPartType.BuyPart:
                        folder = IO.Path.Combine(pnd.WorkingDir.Kaufteile, pnd.Vendor);
                        filename = $"{pnd.Vendor}_{pnd.Partnumber}_{pnd.Description}.ipt";
                        (doc as PartDocument).ComponentDefinition.BOMStructure = BOMStructureEnum.kPurchasedBOMStructure;
                        break;
                    default:
                        return;
                }

                // assign filename and handling code.
                fileName = IO.Path.Combine(folder, filename);
                handlingCode = HandlingCodeEnum.kEventHandled;
            }
        }

        /// <summary>
        /// Handles save as event for IAM assembly's.
        /// </summary>
        /// <param name="doc">document to be saved.</param>
        /// <param name="workpath">Path to working directory (from project settings).</param>
        /// <param name="projectname">Name of current project.</param>
        /// <param name="handlingCode">Reference to handling code result.</param>
        /// <param name="fileName">Reference to result filename variable.</param>
        private void AssemblySaveAsHandler(ref Document doc, string workpath, string projectname, ref HandlingCodeEnum handlingCode, ref string fileName)
        {
            PropertySet designInfo = doc.PropertySets["Design Tracking Properties"];

            // creates a new assembly number dialog
            AsmNumberDialog pnd = new AsmNumberDialog();

            // assign dialog properties from file i-properties.
            pnd.ProjectName = projectname;
            pnd.WorkingDir = new WorkingDir(workpath);
            pnd.Vendor = designInfo["Vendor"].Value as string;
            pnd.Partnumber = designInfo["Part Number"].Value as string;
            pnd.Description = designInfo["Description"].Value as string;

            // show the dialog
            if (pnd.ShowDialog() == true)
            {
                // assign i-properties
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
                        (doc as AssemblyDocument).ComponentDefinition.BOMStructure = BOMStructureEnum.kNormalBOMStructure;
                        break;
                    case EPartType.BuyPart:
                        folder = IO.Path.Combine(pnd.WorkingDir.Kaufteile, pnd.Vendor);
                        filename = $"{pnd.Vendor}_{pnd.Partnumber}_{pnd.Description}.iam";
                        (doc as AssemblyDocument).ComponentDefinition.BOMStructure = BOMStructureEnum.kPurchasedBOMStructure;
                        break;
                    default:
                        return;
                }

                // assign filename and handling code.
                fileName = IO.Path.Combine(folder, filename);
                handlingCode = HandlingCodeEnum.kEventHandled;
            }
        }

        /// <summary>
        /// Handles save as event for drawing documents.
        /// </summary>
        /// <param name="document">document to be saved.</param>
        /// <param name="fileTypes">List of supported file types.</param>
        /// <param name="saveCopyAs">Flag that indicates whether the save as copy option is active.</param>
        /// <param name="workpath">Path to working directory (from project settings).</param>
        /// <param name="handlingCode">Reference to handling code result.</param>
        /// <param name="fileName">Reference to result filename variable.</param>
        private void DrawingSaveAsHandler(ref Document document, string[] fileTypes, bool saveCopyAs, string workpath, ref HandlingCodeEnum handlingCode, ref string fileName)
        {
            // get the current
            DrawingDocument ddoc = document as DrawingDocument;
            if (ddoc == null)
            {
                return;
            }

            // create a new save file dialog
            SaveFileDialog sfd = new SaveFileDialog();

            // applies all file types to filter of dialog
            sfd.Filter = string.Join("|", fileTypes);

            // if save as copy option
            if (saveCopyAs)
            {
                // navigate dialog to "..\DoKumentation\Zeichnungen"
                sfd.InitialDirectory = IO.Path.Combine(IO.Path.GetDirectoryName(workpath), "Dokumentation", "Zeichnungen");

                // filename is the same as the origin document
                sfd.FileName = $"{IO.Path.GetFileNameWithoutExtension(ddoc.FullFileName)}";

                // if PDF is available select PDF as default type
                if (fileTypes.Any(o => o.ToUpper().Contains("PDF")))
                {
                    sfd.FilterIndex = fileTypes.ToList().IndexOf(fileTypes.First(o => o.ToUpper().Contains("PDF"))) + 1;
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
                PropertySet designInfo = this.InventorApplication.ActiveDocument.PropertySets["Design Tracking Properties"];

                // parse part number.
                string partNo = designInfoOrig["Part Number"].Value as string;

                // if part number matches expression
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

                // copy description from document of first view
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
                fileName = sfd.FileName;
                handlingCode = HandlingCodeEnum.kEventHandled;
            }
        }

        private void RefreshPhysical(_Document documentObject, EventTimingEnum beforeOrAfter, NameValueMap context, out HandlingCodeEnum handlingCode)
        {
            if (documentObject is PartDocument && beforeOrAfter == EventTimingEnum.kBefore)
            {
                PartDocument part = documentObject as PartDocument;
                PropertySet invCustomPropertySet = part.PropertySets["Inventor User Defined Properties"];
                Property halbzeugProperty = invCustomPropertySet.OfType<Property>().Any(o => o.Name == "Halbzeug") ?
                    invCustomPropertySet["Halbzeug"] :
                    invCustomPropertySet.Add(string.Empty, "Halbzeug");

                Property overrideDimensions = invCustomPropertySet.OfType<Property>().Any(o => o.Name == "HalbzeugNeuBerechnen") ?
                    invCustomPropertySet["HalbzeugNeuBerechnen"] :
                    invCustomPropertySet.Add(true, "HalbzeugNeuBerechnen");

                Property rotatePart = invCustomPropertySet.OfType<Property>().Any(o => o.Name == "Drehteil") ?
                    invCustomPropertySet["Drehteil"] :
                    invCustomPropertySet.Add(false, "Drehteil");

                part.Update();
                double m = part.ComponentDefinition.MassProperties.Mass;

                if (true.Equals(overrideDimensions.Value))
                {
                    string halbzeug = Routines.DetermineOuterDimensions(part, true.Equals(rotatePart.Value));
                    halbzeugProperty.Value = halbzeug;
                }
            }

            handlingCode = HandlingCodeEnum.kEventNotHandled;
        }
    }
}
