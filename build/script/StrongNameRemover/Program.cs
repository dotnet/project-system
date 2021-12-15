// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace StrongNameRemover
{
    /// <summary>
    /// The main program class of the application.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// This program removes the strong name signature from satellite (localized) assemblies.
        /// The only requirement is to provide the path to the primary assembly as a parameter.
        /// Code based on: https://github.com/dotnet/roslyn-tools/blob/main/src/SignTool/SignTool/SignTool.RealSignTool.cs
        /// </summary>
        /// <param name="args">The arguments for the application.</param>
        /// <returns>0 on success. 1 (or exception) on failure.</returns>
        public static int Main(string[] args)
        {
            if(!args.Any() || args[0] is not string assemblyPath || !File.Exists(assemblyPath))
            {
                Console.WriteLine("Please provide a path to the primary assembly. Satellite assemblies will be discovered automatically.");
                return 1;
            }

            string assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
            string directory = Path.GetDirectoryName(assemblyPath);
            foreach(string satellitePath in Directory.GetFiles(directory, $"{assemblyName}.resources.dll", SearchOption.AllDirectories))
            {
                RemoveStrongNameSignature(satellitePath);
            }

            return 0;
        }

        /// <summary>
        /// The number of bytes from the start of the <see cref="CorHeader"/> to its <see cref="CorFlags"/>.
        /// </summary>
        private const int OffsetFromStartOfCorHeaderToFlags =
               sizeof(int)   // byte count
             + sizeof(short) // major version
             + sizeof(short) // minor version
             + sizeof(long); // metadata directory

        /// <summary>
        /// Returns true if the file provided is an assembly and contains a strong name signature.
        /// </summary>
        /// <remarks>
        /// Cannot add <code>[NotNullWhen(true)]</code> to <paramref name="header"/> until a non-Framework target is used.
        /// See: https://stackoverflow.com/a/61574692/294804
        /// </remarks>
        private static bool IsStrongNameSigned(PEReader peReader, out CorHeader? header)
        {
            header = peReader.PEHeaders.CorHeader;
            if (header == null)
            {
                return false;
            }

            if (!peReader.HasMetadata)
            {
                return false;
            }

            MetadataReader mdReader = peReader.GetMetadataReader();
            if (!mdReader.IsAssembly)
            {
                return false;
            }

            return (header.Flags & CorFlags.StrongNameSigned) == CorFlags.StrongNameSigned;
        }

        /// <summary>
        /// Removes the strong name signature from an assembly. If the assembly has no strong name signature, this simply opens and closes the file with no modifications.
        /// </summary>
        public static void RemoveStrongNameSignature(string assemblyPath)
        {
            using var stream = new FileStream(assemblyPath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
            using var peReader = new PEReader(stream);
            using var writer = new BinaryWriter(stream);

            CorHeader header;
            if (!IsStrongNameSigned(peReader, out header!))
            {
                Console.WriteLine($"No strong name signature for: {assemblyPath}");
                return;
            }

            stream.Position = peReader.PEHeaders.CorHeaderStartOffset + OffsetFromStartOfCorHeaderToFlags;
            writer.Write((uint)(header.Flags & ~CorFlags.StrongNameSigned));
        }
    }
}
