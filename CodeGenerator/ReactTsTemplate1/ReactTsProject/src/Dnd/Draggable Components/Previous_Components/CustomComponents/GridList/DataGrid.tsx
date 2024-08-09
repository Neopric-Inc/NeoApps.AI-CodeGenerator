import React, { useState, useEffect } from 'react'
import { DataGrid } from '@mui/x-data-grid'

const columns = [
    { field: 'id', headerName: 'ID' },
    { field: 'title', headerName: 'Title', width: 300 },
    { field: 'body', headerName: 'Body', width: 600 }
]

const DataGridlist = () => {

    const [tableData, setTableData] = useState([])

    useEffect(() => {
        fetch("https://jsonplaceholder.typicode.com/posts")
            .then((data) => data.json())
            .then((data) => setTableData(data))
    }, [])

   //console.log(tableData)

    return (
        <DataGrid
            rows={tableData}
            columns={columns}
            pageSize={12}
            autoHeight={true}
        />
    )
}

export default DataGridlist;