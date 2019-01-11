# json-ld.net

A [JSON-LD][jsonld] processor for .NET.

[![NuGet][nuget-badge]][nuget]
[![Build Status][travis-badge]][travis]
[![codecov][codecov-badge]][codecov]

This project has adopted the [Microsoft Open Source Code of Conduct][coc].
For more information see the [Code of Conduct FAQ][coc-faq] or contact
[opencode@microsoft.com][ms-mail] with any additional questions or comments.

## Supported frameworks

* .NET 4.0
* .NET Standard 1.3 and 2.0
* Portable .NET 4.5, Windows 8

## Contributing

Pull requests for json-ld.net are welcome, to get started install the latest
tools for .NET Core:

* [.NET Core][dnc]
* [.NET Core tutorial][dnc-tutorial]

### Build and Tests

On Windows, you can execute `build.ps1`, which will create a nupkg and run
tests for both .NET desktop and .NET Core.

On both Windows and all other supported operating systems, you can run
`dotnet build` to build and `dotnet test` to run the tests.

## Origin

This project began life as a [Sharpen][sharpen]-based auto-port from
[jsonld-java][jsonld-java].

  [jsonld]:         https://json-ld.org/
  [sharpen]:        http://community.versant.com/Projects/html/projectspaces/db4o_product_design/sharpen.html
  [jsonld-java]:    https://github.com/jsonld-java/jsonld-java
  [nuget]:          https://www.nuget.org/packages/json-ld.net/
  [nuget-badge]:    https://img.shields.io/nuget/v/json-ld.net.svg
  [coc]:            https://opensource.microsoft.com/codeofconduct/
  [coc-faq]:        https://opensource.microsoft.com/codeofconduct/faq/
  [ms-mail]:        mailto:opencode@microsoft.com
  [dnc]:            https://dot.net
  [dnc-tutorial]:   https://www.microsoft.com/net/core
  [codecov]:        https://codecov.io/gh/linked-data-dotnet/json-ld.net
  [codecov-badge]:  https://img.shields.io/codecov/c/github/linked-data-dotnet/json-ld.net/master.svg
  [travis]:         https://travis-ci.org/linked-data-dotnet/json-ld.net
  [travis-badge]:   https://img.shields.io/travis/linked-data-dotnet/json-ld.net.svg
