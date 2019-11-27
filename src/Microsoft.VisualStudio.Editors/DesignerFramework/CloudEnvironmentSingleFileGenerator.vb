' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.VisualStudio.Editors.DesignerFramework
    ''' <summary>
    ''' Handles running the single-file generator for a particular file path.
    ''' This implementation of <see cref="ISingleFileGenerator"/> handles the "cloud"
    ''' scenario, where the Resource Designer is running on a client system but the
    ''' project system is on a server elsewhere.
    ''' </summary>
    Friend Class CloudEnvironmentSingleFileGenerator
        Implements ISingleFileGenerator

        Public Sub Run() Implements ISingleFileGenerator.Run
            ' Do nothing for the moment.
        End Sub
    End Class
End Namespace

