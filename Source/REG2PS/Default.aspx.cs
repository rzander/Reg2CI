using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using REG2CI;

namespace REG2PS
{
    public partial class Default1 : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void bt_Compile_Click(object sender, EventArgs e)
        {
            try
            {
                REG2CI.RegFile oReg = new RegFile(tb_REG.Text, "REG2PS");
                string sRes = oReg.GetPSCheckAll();
                tb_PSSCript.Text = sRes.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
            }
            catch
            {
                ShowMessage("please upload your Registry content to https://github.com/rzander/Reg2CI/issues ", MessageType.Error);
            }
        }

        protected void bt_GetRemPS_Click(object sender, EventArgs e)
        {
            try
            {
                REG2CI.RegFile oReg = new RegFile(tb_REG.Text, "REG2PS");
                string sRes = oReg.GetPSRemediateAll();
                tb_PSSCript.Text = sRes.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">");
            }
            catch
            {
                ShowMessage("please upload your Registry content to https://github.com/rzander/Reg2CI/issues ", MessageType.Error);
            }
        }

        public enum MessageType { Success, Error, Info, Warning };

        protected void ShowMessage(string Message, MessageType type)
        {
            //ClientScript.RegisterStartupScript(GetType(), "Message", "<SCRIPT LANGUAGE='javascript'>alert('" + Message + "');</script>");
            ScriptManager.RegisterStartupScript(this, this.GetType(), System.Guid.NewGuid().ToString(), "ShowMessage('" + Message + "','" + type + "');", true);
        }
    }
}