// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.ProjectSystem.Debug
{

    // This class is used to serialize the data to/from the json file. 
    // Test Adapter has its own copy of these classes for de-serialization. If there is any change to these classes, 
    // Test Adapter also needs to be updated.
    internal class LaunchSettingsData
    {
        public  Dictionary<string, object> OtherSettings { get; set; }
        
        public List<LaunchProfileData> Profiles { get; set; }

    }

}
