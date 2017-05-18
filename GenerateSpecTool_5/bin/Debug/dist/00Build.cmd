rem --Delete log files--
del /Q /S .\Logs\GenerateSpec\*.* 

rem --Delete the XSD files--
rmdir .\Specification\out\XSD\DataModel /S /Q
rmdir .\Specification\out\XSD\DataModelAnnotated /S /Q
rmdir .\Specification\out\XSD\DataModelNoIncludes /S /Q
rmdir .\Specification\out\XSD\DataModelNoIncludesAnnotated /S /Q
rmdir .\Specification\out\XSD\SIF_Message /S /Q
rmdir .\Specification\out\XSD\SIF_MessageAnnotated /S /Q
rmdir .\Specification\out\XSD\SIF_MessageNoIncludes /S /Q
rmdir .\Specification\out\XSD\SIF_MessageNoIncludesAnnotated /S /Q
rmdir .\Specification\out\XSD\ServiceBodyDefinitions /S /Q

rem --run a version of Spec Gen -- 
generatespec.exe ".\Specification\SIF.Config_DataModel.xml"
