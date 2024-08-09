
import { FlowsAPIService } from "./flowsindex";


export const getAllChartData = () => {
return FlowsAPIService.api().post(`/flows/getDataset`)
}
export const getChartData = (apiPath:string ) => {
  return FlowsAPIService.api().post(apiPath);
}