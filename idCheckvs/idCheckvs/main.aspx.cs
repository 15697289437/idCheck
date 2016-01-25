using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace idCheckvs
{
    public partial class main : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Write(IDCheckUtil.Token);
            Response.Write("<br>");
            string errmsg = "";
            Response.Write(IDCheckUtil.IdCheck("110105199009090018", "张三", ref errmsg));
            Response.Write("<br>");
            Response.Write(errmsg);
            Response.Write("<br>");

            Response.End();
        }
    }
}