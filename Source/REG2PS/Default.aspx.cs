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
            REG2CI.RegFile oReg = new RegFile(tb_REG.Text, "REG2PS");
            tb_PSSCript.Text = oReg.GetPSCheckAll();
        }

        protected void bt_GetRemPS_Click(object sender, EventArgs e)
        {
            REG2CI.RegFile oReg = new RegFile(tb_REG.Text, "REG2PS");
            tb_PSSCript.Text = oReg.GetPSRemediateAll();
        }
    }
}