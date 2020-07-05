# json-ld.net

[![NuGet][nuget-badge]][nuget]
![Build Status][gha-badge]
[![codecov][codecov-badge]][codecov]

## Introduction
This library is an implementation of the JSON-LD specification in C#.

JSON, as specified in RFC7159, is a simple language for representing objects on the Web. Linked Data is a way of describing content across different documents or Web sites. Web resources are described using IRIs, and typically are dereferencable entities that may be used to find more information, creating a "Web of Knowledge". JSON-LD is intended to be a simple publishing method for expressing not only Linked Data in JSON, but for adding semantics to existing JSON.

JSON-LD is designed as a light-weight syntax that can be used to express Linked Data. It is primarily intended to be a way to express Linked Data in JavaScript and other Web-based programming environments. It is also useful when building interoperable Web Services and when storing Linked Data in JSON-based document storage engines. It is practical and designed to be as simple as possible, utilizing the large number of JSON parsers and existing code that is in use today. It is designed to be able to express key-value pairs, RDF data, RDFa data, Microformats data, and Microdata. That is, it supports every major Web-based structured data model in use today.

The syntax does not require many applications to change their JSON, but easily add meaning by adding context in a way that is either in-band or out-of-band. The syntax is designed to not disturb already deployed systems running on JSON, but provide a smooth migration path from JSON to JSON with added semantics. Finally, the format is intended to be fast to parse, fast to generate, stream-based and document-based processing compatible, and require a very small memory footprint in order to operate.

You can read more about JSON-LD on the [JSON-LD website](jsonld).

## Conformance

This library aims to conform with the following:

* [JSON-LD 1.0][JSON-LD 1.0], W3C Recommendation, 2014-01-16, and any [errata][errata]
* [JSON-LD 1.0 Processing Algorithms and API][JSON-LD 1.0 API], W3C Recommendation, 2014-01-16, and any [errata][errata]
* [JSON-LD 1.0 Framing][JSON-LD 1.0 Framing], Unofficial Draft, 2012-08-30
* Working Group [test suite][WG test suite]

The [JSON-LD Working Group][JSON-LD WG] is now developing JSON-LD 1.1. Library
updates to conform with newer specifications will happen as features stabilize
and development time and resources permit.

* [JSON-LD 1.1][JSON-LD WG 1.1], W3C Working Draft, 2018-12-14 or [newer][JSON-LD WG latest]
* [JSON-LD 1.1 Processing Algorithms and API][JSON-LD WG 1.1 API], W3C Working Draft, 2018-12-14 or [newer][JSON-LD WG API latest]
* [JSON-LD 1.1 Framing][JSON-LD WG 1.1 Framing], W3C Working Draft, 2018-12-14 or [newer][JSON-LD WG Framing latest]

The [test runner][] is often updated to note or skip newer tests that are not
yet supported.

## Supported frameworks

* .NET 4.0
* .NET Standard 1.3 and 2.0
* Portable .NET 4.5, Windows 8

## Installation

### dotnet CLI

```sh
dotnet new console
dotnet add package json-ld.net
```

```csharp
using JsonLD.Core;
using Newtonsoft.Json.Linq;
using System;

namespace JsonLD.Demo
{
    internal class Program
    {
        private static void Main()
        {
            var json = "{'@context':{'test':'http://www.test.com/'},'test:hello':'world'}";
            var document = JObject.Parse(json);
            var expanded = JsonLdProcessor.Expand(document);
            Console.WriteLine(expanded);
        }
    }
}

```

## Examples
--------

Example data and context used throughout examples below:

#### doc.json
```json
{
    "@id": "http://example.org/ld-experts",
    "http://schema.org/name": "LD Experts",
    "http://schema.org/member": [{
        "@type": "http://schema.org/Person",
        "http://schema.org/name": "Manu Sporny",
        "http://schema.org/url": {"@id": "http://manu.sporny.org/"},
        "http://schema.org/image": {"@id": "http://manu.sporny.org/images/manu.png"}
    }]
}
```

#### context.json
```json
{
    "name": "http://schema.org/name",
    "member": "http://schema.org/member",
    "homepage": {"@id": "http://schema.org/url", "@type": "@id"},
    "image": {"@id": "http://schema.org/image", "@type": "@id"},
    "Person": "http://schema.org/Person",
    "@vocab": "http://example.org/",
    "@base": "http://example.org/"
}
```


### [compact](http://json-ld.org/spec/latest/json-ld/#compacted-document-form)

```csharp
var doc = JObject.Parse(_docJson);
var context = JObject.Parse(_contextJson);
var opts = new JsonLdOptions();
var compacted = JsonLdProcessor.Compact(doc, context, opts);
Console.WriteLine(compacted);

/*

Output:
{
    "@id": "ld-experts",
    "member": {
        "@type": "Person",
        "image": "http://manu.sporny.org/images/manu.png",
        "name": "Manu Sporny",
        "homepage": "http://manu.sporny.org/"
    },
    "name": "LD Experts",
    "@context": . . .
}

*/
```


### [expand](http://json-ld.org/spec/latest/json-ld/#expanded-document-form)

```csharp
var expanded = JsonLdProcessor.Expand(compacted);
Console.WriteLine(expanded);

/*

Output:
[
    {
        "@id": "http://test.com/ld-experts",
        "http://schema.org/member": [
            {
                "http://schema.org/url": [
                    {
                        "@id": "http://manu.sporny.org/"
                    }
                ],
                "http://schema.org/image": [
                    {
                        "@id": "http://manu.sporny.org/images/manu.png"
                    }
                ],
                "http://schema.org/name": [
                    {
                        "@value": "Manu Sporny"
                    }
                ]
            }
        ],
        "http://schema.org/name": [
            {
                "@value": "LD Experts"
            }
        ]
    }
]

*/


*/
```


### [flatten](http://json-ld.org/spec/latest/json-ld/#flattened-document-form)

```csharp
var doc = JObject.Parse(_docJson);
var context = JObject.Parse(_contextJson);
var opts = new JsonLdOptions();
var flattened = JsonLdProcessor.Flatten(doc, context, opts);
Console.WriteLine(flattened);

/*

Output:
{
    "@context": . . .,
    "@graph": [
        {
            "@id": "_:b0",
            "@type": "Person",
            "image": "http://manu.sporny.org/images/manu.png",
            "name": "Manu Sporny",
            "homepage": "http://manu.sporny.org/"
        },
        {
            "@id": "ld-experts",
            "member": {
                "@id": "_:b0"
            },
            "name": "LD Experts"
        }
    ]
}

*/

```


### [frame](http://json-ld.org/spec/latest/json-ld-framing/#introduction)

For the framing example below, the framing document is defined as follows:

```json
{
    "@context": {
        "name": "http://schema.org/name",
        "member": {"@id": "http://schema.org/member", "@type": "@id"},
        "homepage": {"@id": "http://schema.org/url", "@type": "@id"},
        "image": {"@id": "http://schema.org/image", "@type": "@id"},
        "Person": "http://schema.org/Person"
    },
    "@type": "Person"
}
```

And we use it like this:
```csharp
var doc = JObject.Parse(_docJson);
var frame = JObject.Parse(_frameJson);
var opts = new JsonLdOptions();
var flattened = JsonLdProcessor.Frame(doc, frame, opts);
Console.WriteLine(flattened);

/*

Output:
{
    "@context": . . .,
    "@graph": [
        {
            "@id": "_:b0",
            "@type": "Person",
            "image": "http://manu.sporny.org/images/manu.png",
            "name": "Manu Sporny",
            "homepage": "http://manu.sporny.org/"
        }
    ]
}

*/
```


### <a name="normalize"></a>[normalize](http://json-ld.github.io/normalization/spec/) (aka canonize)

Normalized is a graph of objects that is a canonical representation of the document that can be used for hashing, comparison, etc.

```csharp
var doc = JObject.Parse(_docJson);
var opts = new JsonLdOptions();
var normalized = (RDFDataset)JsonLdProcessor.Normalize(doc, opts);
Console.WriteLine(normalized.Dump());

/*

Output:
@default
    subject
            type      blank node
            value     _:c14n0
    predicate
            type      IRI
            value     http://schema.org/image
    object
            type      IRI
            value     http://manu.sporny.org/images/manu.png
    ---
    subject
            type      blank node
            value     _:c14n0
    predicate
            type      IRI
            value     http://schema.org/name
    object
            type      literal
            value     Manu Sporny
            datatype  http://www.w3.org/2001/XMLSchema#string
    ---
    subject
            type      blank node
            value     _:c14n0
    predicate
            type      IRI
            value     http://schema.org/url
    object
            type      IRI
            value     http://manu.sporny.org/
    ---
    subject
            type      blank node
            value     _:c14n0
    predicate
            type      IRI
            value     http://www.w3.org/1999/02/22-rdf-syntax-ns#type
    object
            type      IRI
            value     http://schema.org/Person
    ---
    subject
            type      IRI
            value     http://example.org/ld-experts
    predicate
            type      IRI
            value     http://schema.org/member
    object
            type      blank node
            value     _:c14n0
    ---
    subject
            type      IRI
            value     http://example.org/ld-experts
    predicate
            type      IRI
            value     http://schema.org/name
    object
            type      literal
            value     LD Experts
            datatype  http://www.w3.org/2001/XMLSchema#string
    ---

*/
```


### <a name="tordf"></a>ToRDF (N-Quads)

```csharp
var doc = JObject.Parse(_docJson);
var opts = new JsonLdOptions();
var rdf = (RDFDataset)JsonLdProcessor.ToRDF(doc, opts);

var serialized = RDFDatasetUtils.ToNQuads(rdf); // serialize RDF to string
Console.WriteLine(serialized);

/*

Output:
<http://example.org/ld-experts> <http://schema.org/member> _:b0 .
<http://example.org/ld-experts> <http://schema.org/name> "LD Experts" .
_:b0 <http://schema.org/image> <http://manu.sporny.org/images/manu.png> .
_:b0 <http://schema.org/name> "Manu Sporny" .
_:b0 <http://schema.org/url> <http://manu.sporny.org/> .
_:b0 <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://schema.org/Person> .

*/
```

_or_ using a custom RDF renderer object

```csharp
private class JSONLDTripleCallback : IJSONLDTripleCallback
{
    public object Call(RDFDataset dataset) =>
        RDFDatasetUtils.ToNQuads(dataset); // serialize the RDF dataset as NQuads
}

internal static void Run()
{
    var doc = JObject.Parse(_docJson);
    var callback = new JSONLDTripleCallback();
    var serialized = JsonLdProcessor.ToRDF(doc, callback);
    Console.WriteLine(serialized);

    /*

    Output:
    <http://example.org/ld-experts> <http://schema.org/member> _:b0 .
    <http://example.org/ld-experts> <http://schema.org/name> "LD Experts" .
    _:b0 <http://schema.org/image> <http://manu.sporny.org/images/manu.png> .
    _:b0 <http://schema.org/name> "Manu Sporny" .
    _:b0 <http://schema.org/url> <http://manu.sporny.org/> .
    _:b0 <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://schema.org/Person> .

    */
}
```

### <a name="fromrdf"></a>fromRDF (N-Quads)

```csharp
var opts = new JsonLdOptions();
var jsonld = JsonLdProcessor.FromRDF(serialized, opts);
Console.WriteLine(jsonld);

/*

Output:
[
    {
        "@id": "_:b0",
        "http://schema.org/image": [
            {
                "@id": "http://manu.sporny.org/images/manu.png"
            }
        ],
        "http://schema.org/name": [
            {
                "@value": "Manu Sporny"
            }
        ],
        "http://schema.org/url": [
            {
                "@id": "http://manu.sporny.org/"
            }
        ],
        "@type": [
            "http://schema.org/Person"
        ]
    },
    {
        "@id": "http://example.org/ld-experts",
        "http://schema.org/member": [
            {
                "@id": "_:b0"
            }
        ],
        "http://schema.org/name": [
            {
                "@value": "LD Experts"
            }
        ]
    }
]
*/
```

_or_ using a custom RDF parser

```csharp
private class CustomRDFParser : IRDFParser
{
    public RDFDataset Parse(JToken input)
    {
        // by public decree, references to example.org are normalized to https going forward...
        var converted = ((string)input).Replace("http://example.org/", "https://example.org/");
        return RDFDatasetUtils.ParseNQuads(converted);
    }
}

internal static void Run()
{
    var parser = new CustomRDFParser();
    var jsonld = JsonLdProcessor.FromRDF(_serialized, parser);
    Console.WriteLine(jsonld);

    /*

    Output:
    [
        {
            "@id": "_:b0",
            "http://schema.org/image": [
                {
                    "@id": "http://manu.sporny.org/images/manu.png"
                }
            ],
            "http://schema.org/name": [
                {
                    "@value": "Manu Sporny"
                }
            ],
            "http://schema.org/url": [
                {
                    "@id": "http://manu.sporny.org/"
                }
            ],
            "@type": [
                "http://schema.org/Person"
            ]
        },
        {
            "@id": "https://example.org/ld-experts",
            "http://schema.org/member": [
                {
                    "@id": "_:b0"
                }
            ],
            "http://schema.org/name": [
                {
                    "@value": "LD Experts"
                }
            ]
        }
    ]
    */
}
```


### Custom documentLoader

```csharp
public class CustomDocumentLoader : DocumentLoader
{
    private static readonly string _cachedExampleOrgContext = Res.ReadString("context.json");

    public override RemoteDocument LoadDocument(string url)
    {
        if (url == "http://example.org/context.jsonld") // we have this cached locally
        {
            var doc = new JObject(new JProperty("@context", JObject.Parse(_cachedExampleOrgContext)));
            return new RemoteDocument(url, doc);
        }
        else
        {
            return base.LoadDocument(url);
        }
    }
}

public static void Run()
{
    var doc = JObject.Parse(_docJson);
    var remoteContext = JObject.Parse("{'@context':'http://example.org/context.jsonld'}");
    var opts = new JsonLdOptions { documentLoader = new CustomDocumentLoader() };
    var compacted = JsonLdProcessor.Compact(doc, remoteContext, opts);
    Console.WriteLine(compacted);

    /*

    Output:
    {
        "@id": "http://example.org/ld-experts",
        "member": {
            "@type": "Person",
            "image": "http://manu.sporny.org/images/manu.png",
            "name": "Manu Sporny",
            "homepage": "http://manu.sporny.org/"
        },
        "name": "LD Experts"
    }

    */
}
```


## Contributing

This project has adopted the [Microsoft Open Source Code of Conduct][coc]. For more information see the [Code of Conduct FAQ][coc-faq] or contact [opencode@microsoft.com][ms-mail] with any additional questions or comments.

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

Documentation for this library is in part drawn from https://github.com/linked-data-dotnet/json-ld.net

  [coc]:                        https://opensource.microsoft.com/codeofconduct/
  [coc-faq]:                    https://opensource.microsoft.com/codeofconduct/faq/
  [codecov]:                    https://codecov.io/gh/linked-data-dotnet/json-ld.net
  [codecov-badge]:              https://img.shields.io/codecov/c/github/linked-data-dotnet/json-ld.net/master.svg

  [ms-mail]:                    mailto:opencode@microsoft.com
  [dnc]:                        https://dot.net
  [dnc-tutorial]:               https://www.microsoft.com/net/core

  [gha-badge]:                  https://github.com/linked-data-dotnet/json-ld.net/workflows/dotnet/badge.svg

  [jsonld]:                     https://json-ld.org/
  [jsonld-java]:                https://github.com/jsonld-java/jsonld-java

  [JSON-LD 1.0]:                http://www.w3.org/TR/2014/REC-json-ld-20140116/
  [JSON-LD 1.0 API]:            http://www.w3.org/TR/2014/REC-json-ld-api-20140116/
  [JSON-LD 1.0 Framing]:        https://json-ld.org/spec/ED/json-ld-framing/20120830/

  [JSON-LD WG 1.1]:             https://www.w3.org/TR/json-ld11/
  [JSON-LD WG 1.1 API]:         https://www.w3.org/TR/json-ld11-api/
  [JSON-LD WG 1.1 Framing]:     https://www.w3.org/TR/json-ld11-framing/

  [JSON-LD WG latest]:          https://w3c.github.io/json-ld-syntax/
  [JSON-LD WG API latest]:      https://w3c.github.io/json-ld-api/
  [JSON-LD WG Framing latest]:  https://w3c.github.io/json-ld-framing/

  [nuget]:                      https://www.nuget.org/packages/json-ld.net/
  [nuget-badge]:                https://img.shields.io/nuget/v/json-ld.net.svg

  [sharpen]:                    http://community.versant.com/Projects/html/projectspaces/db4o_product_design/sharpen.html

  [test runner]:                https://github.com/linked-data-dotnet/json-ld.net/tree/master/test/json-ld.net.tests
  [WG test suite]:              https://github.com/w3c/json-ld-api/tree/master/tests
