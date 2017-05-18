Set objExcel = CreateObject("Excel.Application")

Dim sCurPath
sCurPath = CreateObject("Scripting.FileSystemObject").GetAbsolutePathName(".")

Set objWorkbook = objExcel.Workbooks.Open( sCurPath + "\Specification\out\temp\ImplementationSpecification.xml")
objWorkbook.SaveAs sCurPath + "\Specification\out\ImplementationSpecification.xls", "56" 
objExcel.quit
