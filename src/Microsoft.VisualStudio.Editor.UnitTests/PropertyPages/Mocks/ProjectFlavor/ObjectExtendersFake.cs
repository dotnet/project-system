// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks.ProjectFlavor;

namespace Microsoft.VisualStudio.Editors.UnitTests.PropertyPages.Mocks
{
    class ObjectExtendersFake : ObjectExtenders
    {
        public Dictionary<string, IPropertyFilterOptionsContainer> Fake_RegisteredExtenders = new Dictionary<string, IPropertyFilterOptionsContainer>();

        public ObjectExtendersFake()
        {
        }

#region ObjectExtenders Members

        DTE ObjectExtenders.DTE
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        object ObjectExtenders.GetContextualExtenderCATIDs()
        {
            return new string[] { };
        }

        object ObjectExtenders.GetExtender(string ExtenderCATID, string ExtenderName, object ExtendeeObject)
        {
            return Fake_RegisteredExtenders[ExtenderName];
        }

        object ObjectExtenders.GetExtenderNames(string ExtenderCATID, object ExtendeeObject)
        {
            string[] extenders = new string[Fake_RegisteredExtenders.Count];
            Fake_RegisteredExtenders.Keys.CopyTo(extenders, 0);
            return extenders;
        }

        string ObjectExtenders.GetLocalizedExtenderName(string ExtenderCATID, string ExtenderName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        DTE ObjectExtenders.Parent
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        int ObjectExtenders.RegisterExtenderProvider(string ExtenderCATID, string ExtenderName, IExtenderProvider ExtenderProvider, string LocalizedName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        int ObjectExtenders.RegisterExtenderProviderUnk(string ExtenderCATID, string ExtenderName, IExtenderProviderUnk ExtenderProvider, string LocalizedName)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        void ObjectExtenders.UnregisterExtenderProvider(int Cookie)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #endregion
    }
}
