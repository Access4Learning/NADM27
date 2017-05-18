' del /Q .\Specification\out\temp

del .\Specification\ImplementationSpecification.zip
del .\Specification\out\ImplementationSpecification.zip
.\7za.exe a .\Specification\ImplementationSpecification.zip -r .\Specification\out\
copy .\Specification\ImplementationSpecification.zip .\Specification\out\
del .\Specification\ImplementationSpecification.zip

del .\Specification\SIFSpecification.Out.zip
.\7za.exe a .\Specification\SIFSpecification.Out.zip -r .\Specification\out\

