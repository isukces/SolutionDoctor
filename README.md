# SolutionDoctor
Find and solve common problems with Visual Studio solutions, projects, referenced libraries and NuGet Packages

## How to use?

You can use console application, WPF application is not currently finishied. 

    SolutionDoctor {options} {folder name}
    options:
    -fix      Try to fix errors if possible
    -onlyBig  Show only big problems
    
`folder name` is root folder for scanning. Programm looks for all `*.sln` files in `folder name` and subfolders.

## What kind of problems can I find solve?

Currently *SolutionDoctor* can find following problems

### Solutions in different folders contains the same project

Problem occurs when two or more solutions located in different folders contains reference to the same project (i.e. `csproj` file). It is not big problem unless project uses NuGet packages.

Local NuGet repository location is related to sln file, so we can have many local folders with duplicates of NuGet packages. It is not clear which local folder should be used as as source of referenced files for particular project.
    
This problem leads to confusing messages during project compilation. 

This problem can't be solved automatically.

### Nuget package version and app.config inconsistency

Problem occurs when version used in `app.config` (node  `configuration`.`runtime`.`assemblyBinding`.`assemblyIdentity`) differs from NuGet package 
Nuget package version:

for example
**app.config**

 ```
 <?xml version="1.0" encoding="utf-8"?>
 <packages> 
	...    
	<package id="Newtonsoft.Json" version="6.0.8" targetFramework="net45" />
	...    
</packages>  
  ```  
    
**packages.config**    
    
  ```  
 <?xml version="1.0" encoding="utf-8"?>
 <configuration>
  <runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
      <dependentAssembly>
      ...
        <assemblyIdentity name="Newtonsoft.Json" publicKeyToken="30ad4fe6b2a6aeed" culture="neutral" />
        <bindingRedirect oldVersion="0.0.0.0-6.0.2" newVersion="6.0.2" />
      </dependentAssembly>
      ...
    </assemblyBinding>
  </runtime>       
</configuration>
``` 

This problem can be solved automatically.