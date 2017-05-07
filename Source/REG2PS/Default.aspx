<%@ Page Title="" Language="C#" MasterPageFile="~/Default.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="REG2PS.Default1" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="col-md-10 col-md-offset-1 container">
        <div class="row">
            <div class="col-md-12 form-group">
                <label>Registry:</label>
                <asp:TextBox ID="tb_REG" runat="server" class="form-control" required="required" placeholder="Paste the content of your .reg File here..." Rows="10" TextMode="MultiLine" Font-Bold="False"></asp:TextBox>
            </div>
            <div class="col-md-12 form-group">
                <label>PowerShell:</label>
                <asp:TextBox ID="tb_PSSCript" runat="server" class="form-control" required="required" placeholder="..and you will get the PowerShell code here..." Rows="10" TextMode="MultiLine" BackColor="#012456" ForeColor="White" Font-Names="Helvetica" Font-Bold="False" ReadOnly="true"></asp:TextBox>
            </div>
        </div>
        <br />
        <div>
            <asp:LinkButton ID="bt_Compile" runat="server" CssClass="btn btn-primary" OnClick="bt_Compile_Click">
                <span aria-hidden="true" class="glyphicon glyphicon-send"></span> Get check Script
            </asp:LinkButton>
            <asp:LinkButton ID="bt_GetRemPS" runat="server" CssClass="btn btn-primary" OnClick="bt_GetRemPS_Click">
                <span aria-hidden="true" class="glyphicon glyphicon-send"></span> Get remediation Script
            </asp:LinkButton>
        </div>
        <br />
        <div class="messagealert row" id="alert_container">
        </div>
    </div>
</asp:Content>
