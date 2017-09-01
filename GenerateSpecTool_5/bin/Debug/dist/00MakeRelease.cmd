rem --first run the spec build
%COMSPEC% /C 00build.cmd

rem --this deletes old schema zip files and creates new zip files
%COMSPEC% /C provisionxsd.cmd

rem --make the pdf file.  This hasn't been used in a while
rem %COMSPEC% /C makepdf.vbs 

rem --delete the old Excel file
del .\Specification\out\ImplementationSpecification.xls
rem --creade the new Excel file
%COMSPEC% /C MakeExcelFile.vbs

rem --create the one mega-zip file.  Not done any more
rem %COMSPEC% /C makespecificationzip.cmd