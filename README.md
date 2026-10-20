WebServiceStudio
================

This  project was originally inspired from the .NET Webservice Studio tool and was hosted on codeplex with over 100K downloads.
http://webservicestudio.codeplex.com/

Web Service Studio targets .NET 10 on Windows and uses Windows Forms.

Build with the .NET 10 SDK using `dotnet build WebServiceStudio.sln --configuration Release`.

Web Service Studio is a tool to invoke SOAP web methods interactively. Enter a WSDL URL or select a local WSDL file and click **Get** to discover SOAP operations. Select an operation to create an editable SOAP request template, then click **Invoke** to send it. The request and response can also be edited and sent from the **Raw** tab.

The runtime client supports SOAP 1.1 and SOAP 1.2 document/literal bindings and resolves WSDL and XML Schema imports. Requests are sent as editable XML; generated ASMX proxy classes and their strongly typed parameter editors are no longer used. RPC/encoded bindings, SOAP headers, and WS-Security extensions are not supported.

This tool is meant for webservice implementers to test their webservices without having to write the client code. This could also be used to access other webservices whose WSDL endpoint is known.

Please send your questions / comments / contribution requests to adnan.masood@owasp.org
