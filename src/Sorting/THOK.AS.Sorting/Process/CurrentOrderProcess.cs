using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.MCP;
using THOK.AS.Sorting.Util;
using THOK.AS.Sorting.Dao;
using THOK.AS.Sorting.View;
using THOK.Util;

namespace THOK.AS.Sorting.Process
{
    public class CurrentOrderProcess: AbstractProcess
    {
        private MessageUtil messageUtil = null;

        public override void Initialize(Context context)
        {
            try
            {
                base.Initialize(context);
                messageUtil = new MessageUtil(context.Attributes);
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("CurrentOrderProcess 初始化失败！原因：{0}！ {1}", e.Message, "CurrentOrderProcess.cs 行号：26！"));
            }

        }
        
        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            try
            {              
                string channelGroup = "";

                switch (stateItem.ItemName)
                {
                    case "CurrentOrderA":
                        channelGroup = "A";
                        break;
                    case "CurrentOrderB":
                        channelGroup = "B";
                        break;
                    default:
                        return;
                }

                object o = ObjectUtil.GetObject(stateItem.State);
                if (o != null)
                {
                    string sortNo = o.ToString();       

                    //刷新分拣状态                    
                    Refresh(sortNo,channelGroup);
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("完成订单信息处理失败！原因：{0}！ {1}", e.Message, "CurrentOrderProcess.cs 行号：89！"));
            }
        }

        private void Refresh(string sortNo,string channelGroup)
        {
            try
            {
                using (PersistentManager pm = new PersistentManager())
                {
                    OrderDao orderDao = new OrderDao();
                    string LastCustomerSortNo = orderDao.FindLastCustomerSortNo(channelGroup);
                    if (sortNo == null)
                    {
                        sortNo = orderDao.FindLastSortNo(channelGroup);
                    }

                    //更新完成时间
                    orderDao.UpdateFinisheTime(sortNo, channelGroup);
                    if (sortNo == LastCustomerSortNo)
                    {
                        orderDao.UpdateAllFinisheTime(sortNo, channelGroup);
                    }
                    //刷新主界面分拣状态
                    DataTable infoTable = orderDao.FindOrderInfo(null);
                    RefreshData refreshData = new RefreshData();
                    refreshData.TotalCustomer = Convert.ToInt32(infoTable.Rows[0]["CUSTOMERNUM"]);
                    refreshData.TotalRoute = Convert.ToInt32(infoTable.Rows[0]["ROUTENUM"]);
                    refreshData.TotalQuantity = Convert.ToInt32(infoTable.Rows[0]["QUANTITY"]) + Convert.ToInt32(infoTable.Rows[0]["QUANTITY1"]);

                    infoTable = orderDao.FindOrderInfo(sortNo);
                    refreshData.CompleteCustomer = Convert.ToInt32(infoTable.Rows[0]["CUSTOMERNUM"]);
                    refreshData.CompleteRoute = Convert.ToInt32(infoTable.Rows[0]["ROUTENUM"]);
                    refreshData.CompleteQuantity = Convert.ToInt32(infoTable.Rows[0]["QUANTITY"]) + Convert.ToInt32(infoTable.Rows[0]["QUANTITY1"]);
                    refreshData.Average = orderDao.FindSortingAverage();

                    WriteToProcess("sortingStatus", "RefreshData", refreshData);
                    //messageUtil.SendToSortLed(sortNo, refreshData);
                }
            }
            catch (Exception e)
            {
                Logger.Error(string.Format("更新分拣信息处理失败！原因：{0}！ {1}", e.Message, "CurrentOrderProcess.cs 行号：97！"));
            }
        }
    }
}
