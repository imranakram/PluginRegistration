# The Classic Microsoft Dynamics CRM Plugin Registration Tool
## Now in the XrmToolBox suite

[![Join the chat at https://gitter.im/Innofactor/PluginRegistration](https://badges.gitter.im/Innofactor/PluginRegistration.svg)](https://gitter.im/Innofactor/PluginRegistration?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

##### Project description

![Plugin Registration](https://github.com/Innofactor/PluginRegistration/assets/13014005/edd97502-f897-42ef-b6b8-6f77ac0c9f83)

This is special flavor of classical Plugin Registration Tool provided by Microsoft as CRM SDK code sample. It was adopted to the format of [XrmToolBox](http://www.xrmtoolbox.com/) plugin and itended to work with the same set of the Dynamics CRM versions as supported by main application.

##### Project status

* All main operations are working (but still use carefully on production environments!);
* Azure-related code was stripped away;
* Plugin might 'freeze' for couple seconds during calls to CRM backend (user should be notified about operation performed in `XrmToolBox`-like way);
* Some add-on features Including 
	* Filtering Managed/Unmanaged Assemblies 
	* Exporting Plugin Assemblies, Steps and image information to Excel
	* Filtering Attributes etc.

##### Project needs

* Your bug-reports;
* Your pull-requests;
* Your features suggestions;

##### Running the tests

The unit tests run without any setup. The integration tests (category `Integration`) register a test plug-in assembly in a real Dataverse environment through the tool's own registration code. They create steps, an image and a webhook, check that each one loads back and runs, and then unregister everything again. Use a developer or trial environment, never production.

1. Copy `Xrm.Sdk.PluginRegistration.Tests\testbench.example.json` to `testbench.local.json` in the same folder and fill in a client-secret connection string. That file is gitignored. You can also set `PRT_TESTBENCH` (the path to such a file) or `PRT_TESTBENCH_CONNECTION` (the connection string itself).
2. Build the solution and run the tests from Test Explorer, or with `vstest.console.exe Xrm.Sdk.PluginRegistration.Tests.dll /TestCaseFilter:"TestCategory=Integration"`.

Without a configuration the integration tests are skipped, so CI only runs the unit tests.

Context of original CRM SDK `readme.docx` follows bellow:

---------------------------------------------------------

Microsoft Dynamics CRM 2011 SDK
===============================

Plug-in Registration Tool Readme
--------------------------------

September 2010
--------------

Contents
--------

[Overview](#overview)

[Contents of the Folders](#contents-of-the-folders)

[Installation Instructions](#installation-instructions)

[Important Notes](#important-notes)

[Known Problems](#known-problems)

[Copyright](#copyright)

Overview
--------

The Plug-in Registration sample tool (version 5.0) provides a graphical user interface to register Microsoft Dynamics CRM plug-ins and custom workflow activities. The tool supports the following feature set on the
indicated Microsoft Dynamics CRM deployments.

Microsoft Dynamics CRM 2011

* Register plug-ins and custom workflow activities

Microsoft Dynamics CRM Online

* Register plug-ins

In addition to these features, the tool provides an easy and interactive method to configure [Windows Azure AppFabric](http://www.microsoft.com/windowsazure/appfabric/) ACS for integration with Microsoft Dynamics CRM. Afterwards, you can register a standard plug-in provided with Microsoft Dynamics CRM to post the current platform operation to the AppFabric Service Bus.

The source code to the program is provided to demonstrate how to use the registration classes provided in the Microsoft Dynamics CRM 2011 SDK.

Contents of the Folders
-----------------------

The PluginRegistration folder contains the complete source code and Microsoft Visual Studio 2010 project files for the sample program. The images folder contains bitmap files for the icons used in the program.

Installation Instructions
-------------------------

1.  Download and install [Windows Identity Foundation](http://msdn.microsoft.com/en-us/security/aa570351.aspx).
2.  In Windows Explorer, double-click the PluginRegistrationTool.sln file to open the solution in Visual Studio 2010.
3.  Compile the project by clicking **Build Solution** in the **Build** menu.

Add the Tool to Visual Studio
-----------------------------

You can register the Plug-in Registration Tool as an external tool for Visual Studio 2010.

1.  Open Visual Studio 2010.
2.  Click **Tools**, click **External Tools**, and then click **Add**. If you have not yet added any tools, click [**New Tool 1**].
3.  In the **Title** field, type CRM Plug-in Registration Tool.
4.  In the **Command** field, click the ellipsis (**…**) button and navigate to the PluginRegistration.exe file that you built in the previous task. Click **Open**.
5.  Click **OK** to close the **External Tools** dialog box.

Important Notes
---------------

-   The sample files are not intended to be used in a production environment without prior testing. You should deploy an application that makes use of this sample code to a test environment and examine it for interaction or interference with other parts of the system.
-   Before you deploy applications that make use of this sample code to a production environment, make sure that you consider the existing customizations you may have implemented in Microsoft Dynamics CRM 2011.
-   There is no longer any support in the tool for command line parameters.
-   The tool supports registering plug-ins or custom workflow activities for custom entities.
-   A plug-in assembly named Microsoft.Crm.Extensibility.V3CalloutProxyPlugin.dll may be shown in the list view. However, v3.0 callouts are not supported in Microsoft Dynamics CRM 2011 or Microsoft Dynamics CRM Online.
-   This release of the tool supports registering plug-ins compiled with the Microsoft Dynamics CRM 2011 SDK assemblies. To register plug-ins compiled using the 4.0 SDK assemblies, use the Plug-in Registration Tool included in the 4.0 SDK.

Known Problems
--------------

Copyright 
----------

This document is provided "as-is". Information and views expressed in this document, including URL and other Internet Web site references, may change without notice. You bear the risk of using it.

Some examples depicted herein are provided for illustration only and are fictitious. No real association or connection is intended or should be inferred.

This document does not provide you with any legal rights to any intellectual property in any Microsoft product. You may copy and use this document for your internal, reference purposes.

© 2010 Microsoft Corporation. All rights reserved.

Microsoft, Active Directory, Outlook, Visual Studio, Windows, Windows Server, and Windows Vista are trademarks of the Microsoft group of companies.

All other trademarks are property of their respective owners.
