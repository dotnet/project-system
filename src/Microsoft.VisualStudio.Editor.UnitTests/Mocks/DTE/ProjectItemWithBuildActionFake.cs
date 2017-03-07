using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace Microsoft.VisualStudio.Editors.UnitTests.Mocks
{
    class ProjectItemWithBuildActionFake : ProjectItemFake
    {
        public ProjectItemWithBuildActionFake(string basePath, string relativePath, string initialBuildActionValue)
            : base(basePath, relativePath)
        {
            ItemTypePropertyFake itemTypeProperty = new ItemTypePropertyFake();
            ((Property)itemTypeProperty).Value = initialBuildActionValue;
            BuildActionPropertyFake buildActionProperty = new BuildActionPropertyFake(itemTypeProperty);

            Fake_PropertiesCollection.Fake_PropertiesDictionary.Add("ItemType", itemTypeProperty);
            Fake_PropertiesCollection.Fake_PropertiesDictionary.Add("BuildAction", buildActionProperty);
        }

    }

    class ItemTypePropertyFake : PropertyFake
    {
        public ItemTypePropertyFake()
            : base("ItemType", "None")
        {
        }
    }

    class BuildActionPropertyFake : Property
    {
        private ItemTypePropertyFake m_itemTypeProperty; // This is the backing property

        public BuildActionPropertyFake(ItemTypePropertyFake itemTypeProperty)
        {
            m_itemTypeProperty = itemTypeProperty;
        }

        #region Property Members

        object Property.Application
        {
            get { throw new NotImplementedException(); }
        }

        Properties Property.Collection
        {
            get { throw new NotImplementedException(); }
        }

        DTE Property.DTE
        {
            get { throw new NotImplementedException(); }
        }

        string Property.Name
        {
            get
            {
                return "BuildAction";
            }
        }

        short Property.NumIndices
        {
            get { throw new NotImplementedException(); }
        }

        object Property.Object
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

        Properties Property.Parent
        {
            get { throw new NotImplementedException(); }
        }

        object Property.Value
        {
            get
            {
                switch((string)((Property)m_itemTypeProperty).Value)
                {
                    case "None":
                        return VSLangProj.prjBuildAction.prjBuildActionNone;

                    case "Compile":
                        return VSLangProj.prjBuildAction.prjBuildActionCompile;

                    case "Content":
                        return VSLangProj.prjBuildAction.prjBuildActionContent;

                    case "EmbeddedResource":
                        return VSLangProj.prjBuildAction.prjBuildActionEmbeddedResource;

                    case "ApplicationDefinition":
                        return 123; // Note that in the real project these numbers vary

                    case "Page":
                        return 456;

                    default:
                        return 1234;
                }
            }
            set
            {
                switch ((VSLangProj.prjBuildAction)value)
                {
                    case VSLangProj.prjBuildAction.prjBuildActionNone:
                        ((Property)m_itemTypeProperty).Value = "None";
                        break;

                    case VSLangProj.prjBuildAction.prjBuildActionCompile:
                        ((Property)m_itemTypeProperty).Value = "Compile";
                        break;

                    case VSLangProj.prjBuildAction.prjBuildActionContent:
                        ((Property)m_itemTypeProperty).Value = "Content";
                        break;

                    case VSLangProj.prjBuildAction.prjBuildActionEmbeddedResource:
                        ((Property)m_itemTypeProperty).Value = "EmbeddedResource";
                        break;

                    case (VSLangProj.prjBuildAction)123:
                        ((Property)m_itemTypeProperty).Value = "ApplicationDefinition"; // Note that in the real project these numbers vary
                        break;

                    case (VSLangProj.prjBuildAction)456:
                        ((Property)m_itemTypeProperty).Value = "Page";
                        break;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        object Property.get_IndexedValue(object Index1, object Index2, object Index3, object Index4)
        {
            throw new NotImplementedException();
        }

        void Property.let_Value(object lppvReturn)
        {
            throw new NotImplementedException();
        }

        void Property.set_IndexedValue(object Index1, object Index2, object Index3, object Index4, object Val)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
