# Debugging .NET SDK

## Testing SDK Changes

If you're making changes to the SDK (that is, the [dotnet/sdk](https://github.com/dotnet/sdk) repo) you can easily test VS or msbuild.exe with those changes by setting the `DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR` environment variable.

After you build, find the generated Sdks directory. For example, if your repo is at D:\Projects\sdk, you'll find it at D:\Projects\sdk\bin\Debug\Sdks. Set the environment variable to point to this location:

`set DOTNET_MSBUILD_SDK_RESOLVER_SDKS_DIR=D:\Projects\sdk\bin\Debug\Sdks`

Now any instances of msbuild.exe or VS that inherit that setting will use your locally-produced SDK.