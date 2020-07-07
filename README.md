# json-ld.net

[![NuGet][nuget-badge]][nuget]
![Build Status][gha-badge]
[![codecov][codecov-badge]][codecov]

## Introduction
This library is an implementation of the JSON-LD specification in C#.

JSON, as specified in RFC7159, is a simple language for representing objects on
the Web. Linked Data is a way of describing content across different documents
or Web sites. Web resources are described using IRIs, and typically are
dereferencable entities that may be used to find more information, creating a
"Web of Knowledge". JSON-LD is intended to be a simple publishing method for
expressing not only Linked Data in JSON, but for adding semantics to existing
JSON.

JSON-LD is designed as a light-weight syntax that can be used to express Linked
Data. It is primarily intended to be a way to express Linked Data in JavaScript
and other Web-based programming environments. It is also useful when building
interoperable Web Services and when storing Linked Data in JSON-based document
storage engines. It is practical and designed to be as simple as possible,
utilizing the large number of JSON parsers and existing code that is in use
today. It is designed to be able to express key-value pairs, RDF data, RDFa
data, Microformats data, and Microdata. That is, it supports every major
Web-based structured data model in use today.

The syntax does not require many applications to change their JSON, but easily
add meaning by adding context in a way that is either in-band or out-of-band.
The syntax is designed to not disturb already deployed systems running on JSON,
but provide a smooth migration path from plain JSON to semantically enhanced
JSON. Finally, the format is intended to be fast to parse, fast to generate,
stream-based and document-based processing compatible, and require a very small
memory footprint in order to operate.

You can read more about JSON-LD on the [JSON-LD website][jsonld].

## Conformance

This library aims to conform with the following:

* [JSON-LD 1.0][json-ld-10], W3C Recommendation, 2014-01-16, and any
[errata][errata]
* [JSON-LD 1.0 Processing Algorithms and API][json-ld-10-api], W3C
Recommendation, 2014-01-16, and any [errata][errata]
* [JSON-LD 1.0 Framing][json-ld-10-framing], Unofficial Draft, 2012-08-30
* Working Group [test suite][wg-test-suite]

The [JSON-LD Working Group][json-ld-wg] is now developing JSON-LD 1.1. Library
updates to conform with newer specifications will happen as features stabilize
and development time and resources permit.

* [JSON-LD 1.1][json-ld-wg-11], W3C Working Draft, 2018-12-14 or
[newer][json-ld-wg-latest]
* [JSON-LD 1.1 Processing Algorithms and API][json-ld-wg-11-api], W3C Working
Draft, 2018-12-14 or [newer][json-ld-wg-api-latest]
* [JSON-LD 1.1 Framing][json-ld-wg-11-framing], W3C Working Draft, 2018-12-14
or [newer][json-ld-wg-framing-latest]

The [test runner][test-runner] is often updated to note or skip newer tests that
are not yet supported.

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

### Compact

[Compaction](http://json-ld.org/spec/latest/json-ld/#compacted-document-form) is
the process of applying a developer-supplied context to shorten IRIs to terms or
compact IRIs, and JSON-LD values expressed in expanded form to simple values
such as strings or numbers. Often this makes it simpler to work with a document
as the data is expressed in application-specific terms. Compacted documents are
also typically easier to read for humans.

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

### Expand


[Exapansion](http://json-ld.org/spec/latest/json-ld/#expanded-document-form) is
the process of taking a JSON-LD document and applying a @context such that all
IRIs, types, and values are expanded so that the @context is no longer
necessary.

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

### Flatten

[Flattening](http://json-ld.org/spec/latest/json-ld/#flattened-document-form)
collects all properties of a node in a single JSON object and labels all blank
nodes with blank node identifiers. This ensures a shape of the data and
consequently may drastically simplify the code required to process JSON-LD in
certain applications.

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

### Frame

[Framing](http://json-ld.org/spec/latest/json-ld-framing/#introduction) is used
to shape the data in a JSON-LD document, using an example frame document which
is used to both match the flattened data and show an example of how the
resulting data should be shaped. Matching is performed by using properties
present in the frame to find objects in the data that share common values.
Matching can be done either using all properties present in the frame, or any
property in the frame. By chaining together objects using matched property
values, objects can be embedded within one another.

A frame also includes a context, which is used for compacting the resulting
framed output.

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

### Normalize

[Normalization](http://json-ld.github.io/normalization/spec/) (aka.
canonicalization) converts the document into a graph of objects that is a
canonical representation of the document that can be used for hashing,
comparison, etc.

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

### ToRDF

JSON-LD is a concrete RDF syntax as described in [RDF 1.1 Concepts and Abstract
Syntax][rdf-11-concepts]. Hence, a JSON-LD document is both an RDF document and a
JSON document and correspondingly represents an instance of an RDF data model.
The procedure to deserialize a JSON-LD document to an
[RDF dataset][rdf-11-dataset] (and, optionally, to [RDF N-Quads][n-quads])
involves the following steps:

1. Expand the JSON-LD document, removing any context; this ensures that
properties, types, and values are given their full representation as IRIs and
expanded values.
1. Flatten the document, which turns the document into an array of node objects.
1. Turn each node object into a series of RDF N-Quads.

The processor's `ToRDF` method carries out these steps for you, like this:

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

_or_ using a custom RDF renderer object, like this:

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

### FromRDF

Serialization from RDF N-Quads into JSON-LD can be thought of as the inverse of
the last of the three steps described in summary Deserialization described in
the `ToRDF` method documentation above. Serialization creates an expanded
JSON-LD document closely matching the N-Quads from RDF, using a single node
object for all N-Quads having a common subject, and a single property for those
N-Quads also having a common predicate.

In practice, it looks like this:

_the variable `serialized` is populated with RDF N-Quads values resulting from
the code in the `ToRDF` example above_

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

_or_ using a custom RDF parser:

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

### Custom DocumentLoader

By replacing the default `documentLoader` object placed on the JsonLdProcessor,
it is possible to alter the behaviour when retrieving a remote document (e.g. a
context document) required to execute a given algorithm (e.g. Expansion).

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

This project has adopted the [Microsoft Open Source Code of Conduct][coc]. For
more information see the [Code of Conduct FAQ][coc-faq] or contact
[opencode@microsoft.com][ms-mail] with any additional questions or comments.

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

Documentation for this library is in part drawn from
https://github.com/linked-data-dotnet/json-ld.net

  [coc]:                        https://opensource.microsoft.com/codeofconduct/
  [coc-faq]:                    https://opensource.microsoft.com/codeofconduct/faq/
  [codecov]:                    https://codecov.io/gh/linked-data-dotnet/json-ld.net
  [codecov-badge]:              https://img.shields.io/codecov/c/github/linked-data-dotnet/json-ld.net/master.svg

  [errata]:                     http://www.w3.org/2014/json-ld-errata

  [ms-mail]:                    mailto:opencode@microsoft.com
  [dnc]:                        https://dot.net
  [dnc-tutorial]:               https://www.microsoft.com/net/core

  [gha-badge]:                  https://github.com/linked-data-dotnet/json-ld.net/workflows/dotnet/badge.svg

  [jsonld]:                     https://json-ld.org/
  [jsonld-java]:                https://github.com/jsonld-java/jsonld-java

  [json-ld-10]:                 http://www.w3.org/TR/2014/REC-json-ld-20140116/
  [json-ld-10-api]:             http://www.w3.org/TR/2014/REC-json-ld-api-20140116/
  [json-ld-10-framing]:         https://json-ld.org/spec/ED/json-ld-framing/20120830/

  [json-ld-wg-11]:              https://www.w3.org/TR/json-ld11/
  [json-ld-wg-11-api]:          https://www.w3.org/TR/json-ld11-api/
  [json-ld-wg-11-framing]:      https://www.w3.org/TR/json-ld11-framing/

  [json-ld-wg-latest]:          https://w3c.github.io/json-ld-syntax/
  [json-ld-wg-api-latest]:      https://w3c.github.io/json-ld-api/
  [json-ld-wg-framing-latest]:  https://w3c.github.io/json-ld-framing/

  [n-quads]:                    https://www.w3.org/TR/n-quads/
  [rdf-11-concepts]:            https://www.w3.org/TR/rdf11-concepts/
  [rdf-11-dataset]:             https://www.w3.org/TR/rdf11-concepts/#dfn-rdf-dataset

  [nuget]:                      https://www.nuget.org/packages/json-ld.net/
  [nuget-badge]:                https://img.shields.io/nuget/v/json-ld.net.svg

  [sharpen]:                    http://community.versant.com/Projects/html/projectspaces/db4o_product_design/sharpen.html

  [test-runner]:                https://github.com/linked-data-dotnet/json-ld.net/tree/master/test/json-ld.net.tests
  [wg-test-suite]:              https://github.com/w3c/json-ld-api/tree/master/tests
