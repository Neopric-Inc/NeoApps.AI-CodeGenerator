import * as React from 'react';
import { GridComponent, Sort, Filter, Group, Page, ColumnsDirective, ColumnDirective, Inject, } from '@syncfusion/ej2-react-grids';
import data from './datasource.json';
import './App.css';


export default class Grid extends React.Component<any, any> {
    render() {
        return (
            <div style={{ margin: '10%', marginTop: '5%' }}>
                {/* <center><h1>Grid</h1></center> */}
                <h1>Grid</h1>
                <GridComponent dataSource={data} allowSorting={true} allowPaging={true} pageSettings={{ pageSize: 5 }} allowFiltering={true} allowGrouping={true}>
                    <Inject services={[Sort, Page, Filter, Group]} />
                    <ColumnsDirective>
                        <ColumnDirective field='OrderID' headerText='Invoice ID' textAlign='Right' width='100' />
                        <ColumnDirective field='CustomerID' headerText='Customer ID' width='150' />
                        <ColumnDirective field='OrderDate' headerText='OrderDate' />
                        <ColumnDirective field='ShippedDate' headerText='ShippedDate' />
                        <ColumnDirective field='ShipAddress' headerText='ShipAddress' />
                        <ColumnDirective field='ShipCountry' headerText='Ship Country' />
                        <ColumnDirective field='ShipName' headerText='Ship Name' />
                        <ColumnDirective field='Freight' textAlign='Right' format='C2' width='100' />
                    </ColumnsDirective>

                </GridComponent>
            </div>
        )


    }

}
