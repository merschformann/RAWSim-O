using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RAWSimO.MultiAgentPathFinding.Test
{
    [TestClass]
    public class TestInstanceAreaEraser{

        [TestMethod]
        public void Erase()
        {
            //50x50 with one lane - two dir
            string startX = "13.5";
            string startY = "-1";
            string endX = "38.5";
            string endY = "41.4";
            string filepath = @"C:\Users\ditzel\Desktop\Masterarbeit Repository\Material\Instances\PathFinding\MethodComparison\7_1-7-7-300-654.xinst";

            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = false;
            try { 
                doc.Load(filepath);
                XmlNode instance = doc.GetElementsByTagName("Instance").Item(0);

                if (instance["Semaphores"] != null)
                    instance.RemoveChild(instance["Semaphores"]); 

                //del waypoints
                var allWaypoints = instance.SelectNodes("//Waypoint[@ID]");
                var delWaypoints = instance.SelectNodes("//Waypoint[@ID][@X >= " + startX + "][@Y >= " + startY + "][@X <= " + endX + "][@Y <= " + endY + "]");
                var delInputStations = instance.SelectNodes("//InputStation[@ID][@X >= " + startX + "][@Y >= " + startY + "][@X <= " + endX + "][@Y <= " + endY + "]");
                var delOutputStations = instance.SelectNodes("//OutputStation[@ID][@X >= " + startX + "][@Y >= " + startY + "][@X <= " + endX + "][@Y <= " + endY + "]");
                var delPods = instance.SelectNodes("//Pod[@ID][@X >= " + startX + "][@Y >= " + startY + "][@X <= " + endX + "][@Y <= " + endY + "]");
                var delBots = instance.SelectNodes("//Bot[@ID][@X >= " + startX + "][@Y >= " + startY + "][@X <= " + endX + "][@Y <= " + endY + "]");

                //delete in paths
                foreach (XmlNode node in allWaypoints)
                {
                    var path = node["Paths"];
                    foreach (XmlNode subNode in path.ChildNodes)
                    {
                        foreach (XmlNode delWaypoint in delWaypoints)
                        {
                            if (subNode.InnerText.Equals(delWaypoint.Attributes["ID"].Value))
                            {
                                path.RemoveChild(subNode);
                                break;
                            }
                        }


                    }
                }

                //delete in queue
                foreach (XmlNode inputStation in instance["InputStations"])
                {
                    var queueList = inputStation["Queues"];
                    foreach (XmlNode queue in queueList.ChildNodes)
                    {
                        foreach (XmlNode delWaypoint in delWaypoints)
                        {
                            if (queue.InnerText.Split('/')[1].Equals(delWaypoint.Attributes["ID"].Value))
                            {
                                queueList.RemoveChild(queue);
                                break;
                            }
                        }


                    }
                }

                //delete in queue
                foreach (XmlNode outputStation in instance["OutputStations"])
                {
                    var queueList = outputStation["Queues"];
                    foreach (XmlNode queue in queueList.ChildNodes)
                    {
                        foreach (XmlNode delWaypoint in delWaypoints)
                        {
                            if (queue.InnerText.Split('/')[1].Equals(delWaypoint.Attributes["ID"].Value))
                            {
                                queueList.RemoveChild(queue);
                                break;
                            }
                        }

                    }
                }

                //delete waypoints
                foreach (XmlNode delElement in delWaypoints)
                    instance["Waypoints"].RemoveChild(delElement);

                //delete stations
                foreach (XmlNode delElement in delInputStations)
                    instance["InputStations"].RemoveChild(delElement);
                foreach (XmlNode delElement in delOutputStations)
                    instance["OutputStations"].RemoveChild(delElement);

                //delete Rest
                foreach (XmlNode delElement in delPods)
                    instance["Pods"].RemoveChild(delElement);
                foreach (XmlNode delElement in delBots)
                    instance["Bots"].RemoveChild(delElement);

                doc.Save(filepath + "new.xinst");

                doc = null;
            }
            catch (System.IO.FileNotFoundException)
            {
            }
        }

    }
}
