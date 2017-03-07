using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel;
using System.Reflection;
using VSLangProj80;
using Microsoft.VisualStudio.Editors.UnitTests.Mocks;
using VSLangProj;
using VSLangProj2;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    [CLSCompliant(false)]
    public class ProjectPropertiesFake : ProjectProperties, ProjectProperties2, ProjectProperties3, IVsBrowseObject, ICustomTypeDescriptor, IPropertyContainer
    {
        public IVsHierarchy Fake_hierarchy;
        public Dictionary<string, PropertyFake> Fake_properties = new Dictionary<string, PropertyFake>();
        public MyApplicationPropertiesFake Fake_MyApplicationPropertiesObject = new MyApplicationPropertiesFake();
        public static string CATID_VBProjectBrowseObject = VSLangProj.PrjBrowseObjectCATID.prjCATIDVBProjectBrowseObject;
        public static string CATID_CSharpProjectBrowseObject = VSLangProj.PrjBrowseObjectCATID.prjCATIDCSharpProjectBrowseObject;
        //public static string CATID_JSharpProjectBrowseObject = VSLangProj2.PrjBrowseObjectCATID2.prjCATIDVJSharpProjectBrowseObject;

        private string _categoryID = CATID_VBProjectBrowseObject;

        public ProjectPropertiesFake(IVsHierarchy hierarchy)
        {
            Fake_hierarchy = hierarchy;
            Fake_AddStandardVBProperties();
        }

        public void Fake_AddProperty(string name, object value)
        {
            if (Fake_properties.ContainsKey(name))
            {
                Debug.Fail("Fake property " + name + " already added.");
            }
            Fake_properties.Add(name, new PropertyFake(name, value));
        }

        public void Fake_AddStandardVBProperties()
        {
            // Any properties to be supported must be first added
            Fake_AddProperty("MyType", MyApplication.MyApplicationProperties.Const_MyType_WindowsForms);
            Fake_AddStandardVBMyApplicationProperties();
            Fake_AddProperty("RootNamespace", "WindowsApplication1");
            Fake_AddProperty("OutputType", VSLangProj.prjOutputType.prjOutputTypeWinExe);
            Fake_AddProperty("StartupObject", "Sub Main");
            Fake_AddProperty("AssemblyName", "My.Default.Assembly.Name");
            Fake_AddProperty("ApplicationIcon", "");
            Fake_AddProperty("FullPath", @"c:\temp\WindowsApplication1\");
        }

        public void Fake_AddStandardVBMyApplicationProperties()
        {
            Fake_AddProperty("MyApplication", Fake_MyApplicationPropertiesObject);
        }

        public object Fake_GetPropertyValue(string propertyName)
        {
            return GetPropertyValue(propertyName);
        }

        public void Fake_SetPropertyValue(string propertyName, object value)
        {
            SetPropertyValue(propertyName, value);
        }

        #region ExtenderCATID

        // This one property must be public in order for the AutomationExtenderManager to work properly
        public string ExtenderCATID
        {
            get
            {
                return ((ProjectProperties)this).ExtenderCATID;
            }
        }

        #endregion

        #region Property Get/Set helpers

        private object GetPropertyValue(string propertyName, Type dummyReturnTypeNowObsolete)
        {
            return GetPropertyValue(propertyName);
        }
        private object GetPropertyValue(string propertyName)
        {
            PropertyFake prop = null;
            if (!Fake_properties.TryGetValue(propertyName, out prop))
            {
                Debug.Fail("Property '" + propertyName + "' has not been added to the fake project system");
                throw new ArgumentException("Property '" + propertyName + "' has not been added to the fake project system");
            }

            object value = prop.Value;
            return value;
        }

#if false
        private object GetPropertyValue(string propertyName, Type returnType)
        {
            PropertyFake prop = null;
            if (!Fake_properties.TryGetValue(propertyName, out prop))
            {
#if false 
                prop = new PropertyFake(propertyName, returnType, null);
                Fake_properties.Add(propertyName, prop);
#endif
                throw new ArgumentException("Property '" + propertyName + "' has not been implemented in the fake project system");
            }

            object value = prop.Value;

            if (value == null)
            {
                // Can't convert null to a value type
                if (returnType.IsEnum)
                {
                    value = 0;
                }
                else if (returnType.IsValueType)
                {
                    //NYI value = Convert.ChangeType(0, System.Type.GetTypeCode(returnType.UnderlyingSystemType));
                    value = Convert.ChangeType(0, returnType);
                }
            }
            return value;
        }
#endif

        private void SetPropertyValue(string propertyName, Type dummyExpectedReturnTypeNowObsolete, object value)
        {
            SetPropertyValue(propertyName, value);
        }

        private void SetPropertyValue(string propertyName, object value)
        {
            PropertyFake prop = null;

            ValidatePropertySet(propertyName, value);

            if (!Fake_properties.TryGetValue(propertyName, out prop))
            {
#if false
                prop = new PropertyFake(propertyName, returnType, null);
                Fake_properties.Add(propertyName, prop);
#endif
                Debug.Fail("Property '" + propertyName + "' has not been added to the fake project system");
                throw new ArgumentException("Property '" + propertyName + "' has not been added to the fake project system");
            }

            prop.Value = value;
        }

        /* Doesn't work if the jitter inlines our functions
         
        /// <summary>
        /// Uses reflection to get the curent property value
        /// </summary>
        /// <returns></returns>
        private object GetPropertyValueAutomatically()
        {
            System.Diagnostics.StackFrame sf = (new StackTrace()).GetFrame(1);
            MethodInfo propertyMethod = sf.GetMethod() as MethodInfo;
            Debug.Assert(propertyMethod != null);

            string propertyName = GetPropertyNameFromMethod(propertyMethod);
            Type returnType = propertyMethod.ReturnType;

            return GetProperty(propertyName, returnType);
        }


        /// <summary>
        /// Uses reflection to set the curent property
        /// </summary>
        /// <returns></returns>
        private void SetPropertyValueAutomatically(object value)
        {
            StackFrame sf = (new StackTrace()).GetFrame(1);
            MethodInfo propertyMethod = sf.GetMethod() as MethodInfo;
            Debug.Assert(propertyMethod != null);

            string propertyName = GetPropertyNameFromMethod(propertyMethod);

            Type returnType = propertyMethod.GetParameters()[0].ParameterType;
            SetProperty(propertyName, returnType, value);
        }

        private string GetPropertyNameFromMethod(MethodInfo method)
        {
            const int GetSetPrefixLength = 4; // "_get" or "_set"
            string propertyGetName = method.Name;
            if (propertyGetName.LastIndexOf('.') != 0)
            {
                // C# function that implements an interface member explicitly
                propertyGetName = propertyGetName.Substring(propertyGetName.LastIndexOf('.') + 1);
            }

            string propertyName = propertyGetName.Substring(GetSetPrefixLength);
            if (("get_" + propertyName != propertyGetName)
                && ("set_" + propertyName != propertyGetName))
            {
                //Jitter may munge the name into the form "<propertyname>blahblah"
                if (propertyGetName[0] == '<')
                {
                    propertyName = propertyGetName.Substring(1, propertyGetName.IndexOf('>', 1) - 1);
                    System.Windows.Forms.MessageBox.Show(propertyName);
                }
                else
                {
                    Debug.Fail("Unexpected propertyName format: " + propertyName + " (from " + method.Name + ")");
                }
            }

            return propertyName;
        }
*/

        private void SetPropertyDefaultValue(string name, object value)
        {
            Fake_properties.Add(name, new PropertyFake(name, value));
        }

        #endregion

        #region IPropertyContainer Members

        object IPropertyContainer.GetValue(string propertyName, Type returnType)
        {
            return this.GetPropertyValue(propertyName/*, returnType*/);
        }

        void IPropertyContainer.SetValue(string propertyName, Type returnType, object value)
        {
            this.SetPropertyValue(propertyName, /*returnType, */value);
        }

        #endregion

        #region IVsBrowseObject Members

        int IVsBrowseObject.GetProjectItem(out IVsHierarchy pHier, out uint pItemid)
        {
            pHier = Fake_hierarchy;
            pItemid = (uint)VSITEMID.ROOT;
            return VSConstants.S_OK;
        }

        #endregion



        #region ICustomTypeDescriptor Members - for implementing a fake list of properties

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return new AttributeCollection();
        }

        string ICustomTypeDescriptor.GetClassName()
        {
            return "ProjectPropertiesFake";
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return "ProjectPropertiesFake";
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return null;
        }

        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return null;
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return null;
        }

        object ICustomTypeDescriptor.GetEditor(Type editorBaseType)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(Attribute[] attributes)
        {
            return null;
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return null;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(Attribute[] attributes)
        {
            List<PropertyFakePropertyDescriptor> properties = new List<PropertyFakePropertyDescriptor>();
            foreach (PropertyFake prop in Fake_properties.Values)
            {
                properties.Add(new PropertyFakePropertyDescriptor(prop));
            }
            return new PropertyDescriptorCollection(properties.ToArray());
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[] { });
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }

        #endregion


        #region Validation of specific property sets

        public class ProjectPropertiesFakeInvalidValue : Exception
        {
            public ProjectPropertiesFakeInvalidValue(string propertyName)
                : base("ProjectPropertiesFake: Invalid value for " + propertyName)
            {
            }
        }

        private void ValidateNotNull(string propertyName, object value)
        {
            if (value == null)
                throw new ProjectPropertiesFakeInvalidValue(propertyName);
        }

        private void ValidateNotEmptyString(string propertyName, object value)
        {
            if (string.IsNullOrEmpty((string)value))
                throw new ProjectPropertiesFakeInvalidValue(propertyName);
        }

        private void ValidatePropertySet(string propertyName, object value)
        {
            switch (propertyName)
            {
                case "AssemblyName":
                    ValidateNotEmptyString(propertyName, value);
                    break;
                case "RootNamespace":
                    if (((string)value).IndexOfAny("~!@#$%^&*()_+`-=[]\\{}|'\";:/?,<>".ToCharArray()) > 0)
                        throw new ProjectPropertiesFakeInvalidValue(propertyName);
                    break;
            }
        }

        #endregion

        #region ProjectProperties Members - these members *must* be non-public and *must* be explicitly implemented in order for the project flavor property disabling to work the same as the real deal

        string ProjectProperties.__id
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        object ProjectProperties.__project
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        string ProjectProperties.AbsoluteProjectDirectory
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        VSLangProj.ProjectConfigurationProperties ProjectProperties.ActiveConfigurationSettings
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        string ProjectProperties.ActiveFileSharePath
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        VSLangProj.prjWebAccessMethod ProjectProperties.ActiveWebAccessMethod
        {
            get
            {
                return (prjWebAccessMethod)GetPropertyValue("ActiveWebAccessMethod", typeof(string));
            }
        }

        string ProjectProperties.ApplicationIcon
        {
            get
            {
                return (string)GetPropertyValue("ApplicationIcon", typeof(string));
            }
            set
            {
                SetPropertyValue("ApplicationIcon", typeof(string), value);
            }
        }

        string ProjectProperties.AssemblyKeyContainerName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.AssemblyName
        {
            get
            {
                return (string)GetPropertyValue("AssemblyName", typeof(string)); //NYI: set default
            }
            set
            {
                SetPropertyValue("AssemblyName", typeof(string), value);
            }
        }

        string ProjectProperties.AssemblyOriginatorKeyFile
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj.prjOriginatorKeyMode ProjectProperties.AssemblyOriginatorKeyMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj.prjScriptLanguage ProjectProperties.DefaultClientScript
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj.prjHTMLPageLayout ProjectProperties.DefaultHTMLPageLayout
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.DefaultNamespace
        {
            get
            {
                return (string)GetPropertyValue("DefaultNamespace", typeof(string)); //NYI default
            }
            set
            {
                SetPropertyValue("DefaultNamespace", typeof(string), value);
            }
        }

        VSLangProj.prjTargetSchema ProjectProperties.DefaultTargetSchema
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties.DelaySign
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        object ProjectProperties.get_Extender(string ExtenderName)
        {
            Utility.NotImplemented();
            return null;
        }

        string ProjectProperties.ExtenderCATID
        {
            get
            {
                return _categoryID;
            }
        }

        object ProjectProperties.ExtenderNames
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        string ProjectProperties.FileName
        {
            get
            {
                return (string)GetPropertyValue("FileName", typeof(string));
            }
            set
            {
                SetPropertyValue("FileName", typeof(string), value);
            }
        }

        string ProjectProperties.FileSharePath
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.FullPath
        {
            get
            {
                return (string)GetPropertyValue("FullPath", typeof(string));
            }
        }

        bool ProjectProperties.LinkRepair
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.LocalPath
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.OfflineURL
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        VSLangProj.prjCompare ProjectProperties.OptionCompare
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj.prjOptionExplicit ProjectProperties.OptionExplicit
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj.prjOptionStrict ProjectProperties.OptionStrict
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.OutputFileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj.prjOutputType ProjectProperties.OutputType
        {
            get
            {
                return (VSLangProj.prjOutputType)GetPropertyValue("OutputType", typeof(string));
            }
            set
            {
                SetPropertyValue("OutputType", typeof(VSLangProj.prjOutputType), value);
            }
        }

        VSLangProj.prjProjectType ProjectProperties.ProjectType
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.ReferencePath
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.RootNamespace
        {
            get
            {
                return (string)GetPropertyValue("RootNamespace", typeof(string));
            }
            set
            {
                SetPropertyValue("RootNamespace", typeof(string), value);
            }
        }

        string ProjectProperties.ServerExtensionsVersion
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        string ProjectProperties.StartupObject
        {
            get
            {
                return (string)GetPropertyValue("StartupObject", typeof(string));
            }
            set
            {
                SetPropertyValue("StartupObject", typeof(string), value);
            }
        }

        string ProjectProperties.URL
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        VSLangProj.prjWebAccessMethod ProjectProperties.WebAccessMethod
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties.WebServer
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        string ProjectProperties.WebServerVersion
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        #endregion

        #region ProjectProperties2 implementation - these members *must* be non-public and *must* be explicitly implemented in order for the project flavor property disabling to work the same as the real deal

        string ProjectProperties2.AbsoluteProjectDirectory
        {
            get
            {
                return ((ProjectProperties)this).AbsoluteProjectDirectory;
            }
        }

        ProjectConfigurationProperties ProjectProperties2.ActiveConfigurationSettings
        {
            get { return ((ProjectProperties)this).ActiveConfigurationSettings; }
        }

        string ProjectProperties2.ActiveFileSharePath
        {
            get { return ((ProjectProperties)this).ActiveFileSharePath; }
        }

        prjWebAccessMethod ProjectProperties2.ActiveWebAccessMethod
        {
            get { return ((ProjectProperties)this).ActiveWebAccessMethod; }
        }

        string ProjectProperties2.ApplicationIcon
        {
            get
            {
                return ((ProjectProperties)this).ApplicationIcon;
            }
            set
            {
                ((ProjectProperties)this).ApplicationIcon = value;
            }
        }

        string ProjectProperties2.AssemblyKeyContainerName
        {
            get
            {
                return ((ProjectProperties)this).AssemblyKeyContainerName;
            }
            set
            {
                ((ProjectProperties)this).AssemblyKeyContainerName = value;
            }
        }

        string ProjectProperties2.AssemblyName
        {
            get
            {
                return ((ProjectProperties)this).AssemblyName;
            }
            set
            {
                ((ProjectProperties)this).AssemblyName = value;
            }
        }

        string ProjectProperties2.AssemblyOriginatorKeyFile
        {
            get
            {
                return ((ProjectProperties)this).AssemblyOriginatorKeyFile;
            }
            set
            {
                ((ProjectProperties)this).AssemblyOriginatorKeyFile = value;
            }
        }

        prjOriginatorKeyMode ProjectProperties2.AssemblyOriginatorKeyMode
        {
            get
            {
                return ((ProjectProperties)this).AssemblyOriginatorKeyMode;
            }
            set
            {
                ((ProjectProperties)this).AssemblyOriginatorKeyMode = value;
            }
        }

        prjScriptLanguage ProjectProperties2.DefaultClientScript
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        prjHTMLPageLayout ProjectProperties2.DefaultHTMLPageLayout
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties2.DefaultNamespace
        {
            get
            {
                return ((ProjectProperties)this).DefaultNamespace;
            }
            set
            {
                ((ProjectProperties)this).DefaultNamespace = value;
            }
        }

        prjTargetSchema ProjectProperties2.DefaultTargetSchema
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        bool ProjectProperties2.DelaySign
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties2.ExtenderCATID
        {
            get { return ((ProjectProperties)this).ExtenderCATID; }
        }

        object ProjectProperties2.ExtenderNames
        {
            get { return ((ProjectProperties)this).ExtenderNames; }
        }

        string ProjectProperties2.FileName
        {
            get
            {
                return ((ProjectProperties)this).FileName;
            }
            set
            {
                ((ProjectProperties)this).FileName = value;
            }
        }

        string ProjectProperties2.FileSharePath
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties2.FullPath
        {
            get { return ((ProjectProperties)this).FullPath; }
        }

        bool ProjectProperties2.LinkRepair
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties2.LocalPath
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties2.OfflineURL
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        prjCompare ProjectProperties2.OptionCompare
        {
            get
            {
                return ((ProjectProperties)this).OptionCompare;
            }
            set
            {
                ((ProjectProperties)this).OptionCompare = value;
            }
        }

        prjOptionExplicit ProjectProperties2.OptionExplicit
        {
            get
            {
                return ((ProjectProperties)this).OptionExplicit;
            }
            set
            {
                ((ProjectProperties)this).OptionExplicit = value;
            }
        }

        prjOptionStrict ProjectProperties2.OptionStrict
        {
            get
            {
                return ((ProjectProperties)this).OptionStrict;
            }
            set
            {
                ((ProjectProperties)this).OptionStrict = value;
            }
        }

        string ProjectProperties2.OutputFileName
        {
            get { return ((ProjectProperties)this).OutputFileName; }
        }

        prjOutputType ProjectProperties2.OutputType
        {
            get
            {
                return ((ProjectProperties)this).OutputType;
            }
            set
            {
                ((ProjectProperties)this).OutputType = value;
            }
        }

        prjProjectType ProjectProperties2.ProjectType
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties2.ReferencePath
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties2.RootNamespace
        {
            get
            {
                return ((ProjectProperties)this).RootNamespace;
            }
            set
            {
                ((ProjectProperties)this).RootNamespace = value;
            }
        }

        string ProjectProperties2.ServerExtensionsVersion
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties2.StartupObject
        {
            get
            {
                return ((ProjectProperties)this).StartupObject;
            }
            set
            {
                ((ProjectProperties)this).StartupObject = value;
            }
        }

        string ProjectProperties2.URL
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        prjWebAccessMethod ProjectProperties2.WebAccessMethod
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties2.WebServer
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties2.WebServerVersion
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties2.__id
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ProjectProperties2.__project
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ProjectProperties2.get_Extender(string ExtenderName)
        {
            return ((ProjectProperties)this).get_Extender(ExtenderName);
        }

        string ProjectProperties2.AspnetVersion
        {
            get
            {
                Utility.NotImplemented();
                return null;
            }
        }

        string ProjectProperties2.PostBuildEvent
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties2.PreBuildEvent
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        VSLangProj2.prjRunPostBuildEvent ProjectProperties2.RunPostBuildEvent
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region ProjectProperties3 Members - these members *must* be non-public and *must* be explicitly implemented in order for the project flavor property disabling to work the same as the real deal


        string ProjectProperties3.AssemblyFileVersion
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.AssemblyGuid
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.AssemblyKeyProviderName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        uint ProjectProperties3.AssemblyOriginatorKeyFileType
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        prjAssemblyType ProjectProperties3.AssemblyType
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.AssemblyVersion
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties3.ComVisible
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Company
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Copyright
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.DebugSecurityZoneURL
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Description
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties3.EnableSecurityDebugging
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.ExcludedPermissions
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties3.GenerateManifests
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.ManifestCertificateThumbprint
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.ManifestKeyFile
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.ManifestTimestampUrl
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.NeutralResourcesLanguage
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Product
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        object ProjectProperties3.Publish
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties3.SignAssembly
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties3.SignManifests
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.TargetZone
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Title
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Trademark
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        bool ProjectProperties3.TypeComplianceDiagnostics
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.Win32ResourceFile
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        string ProjectProperties3.AbsoluteProjectDirectory
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        ProjectConfigurationProperties ProjectProperties3.ActiveConfigurationSettings
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.ActiveFileSharePath
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        prjWebAccessMethod ProjectProperties3.ActiveWebAccessMethod
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.ApplicationIcon
        {
            get
            {
                return ((ProjectProperties2)this).ApplicationIcon;
            }
            set
            {
                ((ProjectProperties2)this).ApplicationIcon = value;
            }
        }

        string ProjectProperties3.AspnetVersion
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.AssemblyKeyContainerName
        {
            get
            {
                return ((ProjectProperties2)this).AssemblyKeyContainerName;
            }
            set
            {
                ((ProjectProperties2)this).AssemblyKeyContainerName = value;
            }
        }

        string ProjectProperties3.AssemblyName
        {
            get
            {
                return ((ProjectProperties2)this).AssemblyName;
            }
            set
            {
                ((ProjectProperties2)this).AssemblyName = value;
            }
        }

        string ProjectProperties3.AssemblyOriginatorKeyFile
        {
            get
            {
                return ((ProjectProperties2)this).AssemblyOriginatorKeyFile;
            }
            set
            {
                ((ProjectProperties2)this).AssemblyOriginatorKeyFile = value;
            }
        }

        prjOriginatorKeyMode ProjectProperties3.AssemblyOriginatorKeyMode
        {
            get
            {
                return ((ProjectProperties2)this).AssemblyOriginatorKeyMode;
            }
            set
            {
                ((ProjectProperties2)this).AssemblyOriginatorKeyMode = value;
            }
        }

        prjScriptLanguage ProjectProperties3.DefaultClientScript
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        prjHTMLPageLayout ProjectProperties3.DefaultHTMLPageLayout
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.DefaultNamespace
        {
            get
            {
                return ((ProjectProperties2)this).DefaultNamespace;
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        prjTargetSchema ProjectProperties3.DefaultTargetSchema
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        bool ProjectProperties3.DelaySign
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.ExtenderCATID
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ProjectProperties3.ExtenderNames
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.FileName
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.FileSharePath
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.FullPath
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        bool ProjectProperties3.LinkRepair
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.LocalPath
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.OfflineURL
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        prjCompare ProjectProperties3.OptionCompare
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        prjOptionExplicit ProjectProperties3.OptionExplicit
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        prjOptionStrict ProjectProperties3.OptionStrict
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.OutputFileName
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        prjOutputType ProjectProperties3.OutputType
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.PostBuildEvent
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.PreBuildEvent
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        prjProjectType ProjectProperties3.ProjectType
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.ReferencePath
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.RootNamespace
        {
            get
            {
                return ((ProjectProperties2)this).RootNamespace;
            }
            set
            {
                ((ProjectProperties2)this).RootNamespace = value;
            }
        }

        prjRunPostBuildEvent ProjectProperties3.RunPostBuildEvent
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.ServerExtensionsVersion
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.StartupObject
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.URL
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        prjWebAccessMethod ProjectProperties3.WebAccessMethod
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        string ProjectProperties3.WebServer
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.WebServerVersion
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        string ProjectProperties3.__id
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ProjectProperties3.__project
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ProjectProperties3.get_Extender(string ExtenderName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion




    }
}
