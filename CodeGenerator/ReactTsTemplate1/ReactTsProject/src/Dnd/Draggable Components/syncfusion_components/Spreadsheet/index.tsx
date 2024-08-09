import * as ReactDOM from 'react-dom';
import * as React from 'react';
import { SpreadsheetComponent, SheetsDirective, SheetDirective, ColumnsDirective, RangesDirective, RangeDirective, RowsDirective, RowDirective, CellsDirective, CellDirective, CellStyleModel, ColumnDirective } from '@syncfusion/ej2-react-spreadsheet';
import { defaultData } from './data';
import './spreadsheet.css';

/**
 * Default Spreadsheet sample
 */

export const Spreadsheet = () => {
    let spreadsheet: SpreadsheetComponent;
    const boldRight: CellStyleModel = { fontWeight: 'bold', textAlign: 'right' };
    const bold: CellStyleModel = { fontWeight: 'bold' };

    function onCreated(): void {
        spreadsheet.cellFormat({ fontWeight: 'bold', textAlign: 'center', verticalAlign: 'middle' }, 'A1:F1');
        spreadsheet.numberFormat('$#,##0.00', 'F2:F31');
    }

    return (
        <div className='control-pane'>
            <h1>SpreadSheet</h1>
            <div className='control-section spreadsheet-control'>
                <SpreadsheetComponent openUrl='https://ej2services.syncfusion.com/production/web-services/api/spreadsheet/open'
                    saveUrl='https://ej2services.syncfusion.com/production/web-services/api/spreadsheet/save'
                    ref={(ssObj) => { spreadsheet = ssObj }} created={onCreated.bind(this)} >
                    <SheetsDirective>
                        <SheetDirective name="Car Sales Report">
                            <RangesDirective>
                                <RangeDirective dataSource={defaultData}></RangeDirective>
                            </RangesDirective>
                            <RowsDirective>
                                <RowDirective index={30}>
                                    <CellsDirective>
                                        <CellDirective index={4} value="Total Amount:" style={boldRight}></CellDirective>
                                        <CellDirective formula="=SUM(F2:F30)" style={bold}></CellDirective>
                                    </CellsDirective>
                                </RowDirective>
                            </RowsDirective>
                            <ColumnsDirective>
                                <ColumnDirective width={180}></ColumnDirective>
                                <ColumnDirective width={130}></ColumnDirective>
                                <ColumnDirective width={130}></ColumnDirective>
                                <ColumnDirective width={180}></ColumnDirective>
                                <ColumnDirective width={130}></ColumnDirective>
                                <ColumnDirective width={120}></ColumnDirective>
                            </ColumnsDirective>
                        </SheetDirective>
                    </SheetsDirective>
                </SpreadsheetComponent>
            </div>
        </div>
    )
}


