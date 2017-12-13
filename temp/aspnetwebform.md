ASP.NET Web Form
## Profile
对象是Web Form， MVC和ajax的细节会有所不同
Chapter 32
介绍使用ASP.NET进行web应用开发.ASP.NET网页的构造基础的编程模型.web服务器的选择和web.config文件的使用
Chapter 33
组成内部控件树的控件:验证控件，内置站点导航控件及数据绑定操作在内的核心web控件
Chapter 34
.NET处理状态管理的多种方式:
    基于客户端的状态管理：view state， control state，  hidden Fields, cookies， Qurey Strings, 
    基于服务器端的状态管理： session state，application state, profile properties
    及应用程序高速缓存

### 32 ASP.NET Web Form
Web中的关键概念:HTTP,HTML, 客户端脚本和回送，IIS的作用及ASP.NET Development Web Server
接下来包括ASP.NET 表单编程模型(单页面和代码隐藏页面)并研究Page基类的功能.
1. HTTP的作用 Hypertext tranfer protocol
用于web应用程序之间进行发送和接收数据的协议
HTTP是以文本为基础的协议，建立在标准的request/response范型上.本身是无序的
    1. HTTP Request/Response
    浏览器: 输入网址:http://www.baidu.com => DNS(域名系统)转换成IP:103.235.46.39 => 浏览器打开一个socket(套接字接口，默认80)，作为浏览器程序与服务器之间连接的接口
    web服务器:接收到http请求，根据文件类型加载到对应的编程语言解释器 => 解释器处理请求及对应的page code=> 根据源码获取资源，服务器上的文件，数据库中的数据 => 返回结果到服务器 => 服务器返回结果response page到浏览器
        `[IIS](https://docs.microsoft.com/en-us/iis/get-started/introduction-to-iis/introduction-to-iis-architecture#Hypertext): 
        request => Protocol Listeners: HTTP.sys(HTTP Protocol Stack) 
        => WAS(Windows Process Activation Service) and W3SVC(World Wide Web Publishing Service) based on svchost.exe configure http.sys 
        => WAS run a W3WP.exe(worker precess) 
        => handle by Module application pool 
        => HTTP.sys`
    2. HTTP是无状态协议，browser/server并不知道request/response的先后，解决:cookie，session，cache
2. Web应用程序和Web服务器
web应用程序包括各种文件(\*.html, \*.ASPx, image file, xml file..)和存储在制定web服务骑上特定目录集内相关组件(.NET代码库)的集合.
web服务器就是承载web应用程序的软件产品.IIS是微软的服务器
    1. 虚拟目录:驻留一个web应用程序
    2. ASP.NET Development Web Server: 轻量级web服务器，允许开发者在IIS范围之外承载ASP.NET web应用程序.
3. HTML的作用
    1. browser prase html file and show content. [learning code](https://github.com/Song2017/SourceCode/tree/master/HTML5_CSS3)
    2. html build document bone, css fill fresh, javascript interact with user.
4. Script的作用
    1. 脚本可以直接由browser处理:1， 校验用户输入；2， 与DOM交互
    2. browser解析HTML时会在内存中建立一个对象树，表示Web页面中的内容
browser提供了操作对象树的API:DOM，他公开了对象树并允许以编程的方式修改.常用的编程语言就是JavaScript
DOM的功能类似，但是不同browser的对象模型并不一致.ASP.NET属性HttpRequest.Browser，允许在运行时确定发送当前请求的浏览器
Parsing and Render engines: ie, Trident; google: KHTML engine in KDE's Konqueror; Gecko, firefox; Presto engine, Opera;

            <input id="btnShow" type="button" value="Show" onclick="return btnShow_onClick()" />
            <script type="text/javascript">
                function btnShow_onClick() {
                    alert(textUserMessage.value);
                }
            </script>
5. 回发到web server
   get: 表单数据会附加到url字符串中，可见且有长度限制，get是幂等的
   post:表单数据对外界数据不可见，没有长度限制，不是幂等的. put是幂等
    
        <form id="defaultPage"
              action="http://localhost/cars/classicASPPage.ASP" method="post">
            <input id="btnPostBack" type="submit" value="Post to server" />
        </form>

6. ASP.NET API
    1. 代码隐藏编程模型:表现逻辑(html)从业务逻辑(c#)中分离出来
    2. 使用.NET编程语言而不是服务器段脚本语言，代码文件可以编译为dll程序集
    3. web.config可以配置web程序
    4. version 2.0:  master page: 适用于统一外观的页面
        version 3.5: linq
        version 4.0 4.5:support html 5，asynchronous
7. build ASP.NET single web page
    1. ASP.NET指令的作用
    
            <%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.ASPx.cs" Inherits="ASPOutOfCache.Views.WebForm1" %>
            <%@ Import Namespace="System.Data" %>
    2. 服务器特性: runat="server"
    3. ASP.NET控件
    继承自System.Object，ASP.NET控件的共同父类为webcontrol
9. ASP.NET web site vs ASP.NET web application
    ASP.NET web application: 发布前进行预编译
    ASP.NET web site: 隐藏*.ASPx.designer.cs以支持分部类， 可以按原样发布
10. ASP.NET 网站目录结构
    1. App_Code:组件或类.cs,.vb等的源代码文件夹，对应用程序是自动可访问的.所有的代码文件会被编译到一个程序集中.A.cs and A.vb lead to error
    2. Bin: 存放经过编译的dll文件，仅当前的应用程序自动调用可以自动调用
    1. App_Data: .mdb,.mdf,xml或其他数据文件
    1. App_GloabalResources: .resx文件，作用范围是应用程序
    1. App_LocalResources: .resx文件，作用范围是特定网页
11. 页面类型的继承链
    1.  .NET网页 is-a System.Web.UI.Page is-a TemplateControl is-a Control is-a Object
    Cache: 允许当前站点与高速缓存对象交互. Cache可以设置缓存位置，有效时间及线程安全
    ClientTarget: set/get browser type
    IsPostBack: 获得request是否是post返回方法
12. 与传入的HTTP 请求交互 Request
    1. Browser: 获取发出请求的浏览器的信息
    ```public HttpBrowserCapabilities Browser { get; set; }```
    2. Form/QuerySreing: 获取browser通过post/get方法发送的信息
    1. Page.IsPostBack: server用来判断浏览器中page的属性.如果状态发生了改变，即为true，否则，第一次加载时，page没有被操作过，为false
13. 与输出的HTTP响应交互 Response
    1. 向http输出流中写内容```Response.Write("this is write by response");```
    2. 重定向用户```Response.Redirect("http://www.baidu.com");```
14. ASP.NET网页的生命周期
    1.  PreInit 分配web控件，应用主题，master page
    2.  Init 把web控件的属性设为他们原先的值
    3.  Load 页面与控件完全初始化，此时可以与web窗口控件进行交互
    4.  引发回传的事件 按钮单击..
    5.  PreRender 控件的数据绑定与UI配置已经完成，控件准备好将自身的数据呈现到将要出现的HTTP response里
    6.  Unload 页面与控件已经完成了呈现过程，页面对象将被销毁，此时不能与response进行交互，主要用来进行页面曾的清除.关闭文件或数据库连接等
    7.  AutoEventWireUp特性: true:页面级别的事件会被自动处理 
    8.  Error事件:默认的server报错时调用的方法
    
            protected void Page_Error(object sender, EventArgs e)
            {
                Response.Clear();
                Response.Write("the page you requested is not exists..");
                Response.Write($"{ Server.GetLastError().ToString()}");
                Server.ClearError();//不再显示默认报错页面
            }
15. web.config文件的作用
    1.  web.config: 用来指导CLR处理绑定请求，程序集探查及其他运行时细节的XML配置文件
    1.  元素
        1.  appsettings:自定义键值对
        1.  authentication: web程序的用户验证
        1.  authorization: 资源的访问权限验证
        1.  connectionstrings: 连接字符串
        1.  sessionstate:用于控制session状态数据的存储





### 33 ASP.NET Web 控件，master page和主题
介绍构成页面用户界面Web控件的细节
1. web控件的本质
asp.net控件继承自System。Web。UI。WebControls，每种控件对应一个类，可以用OO方式操作。 基类是System。Web。UI。WebControls。WebControl
    1. asp.net web控件公开一个有限的事件集， 这些事件最终长生到web服务器的回传
    2. AutoPostBack属性
        默认为false。
        true时将会根据浏览器端操纵触发对应服务器端事件。
2. Control和WebControl基类
    1. Controls： 获取一个ControlCollection对象，表示当前控件的各个子控件
            foreach (Control c in Panel1.Controls)
            if (ReferenceEquals(c.GetType(), typeof(Label)))
            {
                Label label = c as Label;
                strTemp += $"contrl name is {label.Text.ToString()};";
            }
    2. WebControl基类提供一个到所有图形多态接口
        BackColor，Font， Enabled
3. web控件的类别
    sitemap: 浏览路径

4. 构建asp.net网站
    1. master page： 
        ContentPlaceHolder: 为子页面预留
        
        <%@ Master Language="C#" AutoEventWireup="true" CodeFile="MasterPage.master.cs" Inherits="MasterPage" %>
        <!DOCTYPE html>
        <html>
        <head runat="server">
            <asp:ContentPlaceHolder ID="head" runat="server">
            </asp:ContentPlaceHolder>
        </head>
        <body>
            <form id="form1" runat="server">
                <div>
                    <asp:ContentPlaceHolder ID="ContentPlaceHolder1" runat="server">
                    </asp:ContentPlaceHolder>
                </div>
            </form>
        </body>
        </html>
    2. SiteMap: 站点导航控件
        MasterPageFile：制定masterpage文件
        ContentPlaceHolderID： 关联masterpage预留占位符
        
        <siteMapNode url="~/Default.aspx" title="Home"  description="the home page" />
        <%@ Page Title="" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="Default" %>
        <asp:Content ID="Content1" ContentPlaceHolderID="head" Runat="Server">
            <title>
                this is Default page 
            </title> 
        </asp:Content>
        <asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" Runat="Server">
            Welcome to Car websites
        </asp:Content>
    3. GridView: 表格控件
    4. Wiazrd： 向导安装控件

        <asp:Wizard ID="Wizard1" runat="server" Width="669px" OnFinishButtonClick="Wizard1_FinishButtonClick">
            <WizardSteps>
                <asp:WizardStep ID="step1" runat="server" Title="Pick Your Model">
                    <asp:TextBox ID="TextBox1" runat="server">default model</asp:TextBox>
                </asp:WizardStep>
                <asp:WizardStep ID="step2" runat="server" Title="Pick Your Color">
                    <asp:ListBox ID="ListBox1" runat="server" Width="237px">
                        <asp:ListItem>purple</asp:ListItem>
                        <asp:ListItem>green</asp:ListItem>
                        <asp:ListItem>red</asp:ListItem>
                        <asp:ListItem>yellow</asp:ListItem>
                        <asp:ListItem>white</asp:ListItem>
                        <asp:ListItem>black</asp:ListItem>
                    </asp:ListBox>
                </asp:WizardStep>
                <asp:WizardStep ID="step3" runat="server" Title="Name Your Car">
                    <asp:TextBox ID="TextBox2" runat="server">default name</asp:TextBox>
                </asp:WizardStep>
                <asp:WizardStep ID="step4" runat="server" Title="Delivery Date">
                    <asp:Calendar ID="Calendar1" runat="server"></asp:Calendar>
                </asp:WizardStep>
            </WizardSteps>
        </asp:Wizard>

        //完成按钮单击事件
        protected void Wizard1_FinishButtonClick(object sender, WizardNavigationEventArgs e)
        {
            string _temp;
            _temp = $"{TextBox1.Text.ToString()},{ListBox1.SelectedValue.ToString()},{TextBox2.Text.ToString()}" +
                $",{Calendar1.SelectedDate.ToLongTimeString()}";
            LabelOrder.Text = _temp;
        }
5. 验证控件
    validation control派生自基类System.Web.UI.WebControls.BaseValidator，客户端过滤无意义输入信息
    HTML5 提供默认的控件验证
    <input type="email/number/tel/url.." name=emailInput required></input>

