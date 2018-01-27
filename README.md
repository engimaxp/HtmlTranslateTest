# C# 实现Html转JSON #

Html为树结构->Json为数组结构

## 应用场景 ##

H5或浏览器展示Html代码没有问题，但是让原生APP或ReactNative直接展示Html可能会有很多不便

## 实现方法 ##

可以通过正则表达式捕获、替换，但是文本类型复杂的话，正则表达式的复杂度就会直线上升，所以这里考虑采用更灵活的实现方式

### 1 将Html字符串文本解析为树结构对象HtmlNode ###

#### 使用正则表达式或第三方框架确定并定位各节点首位未知 ####

##### 方法一 正则表达式匹配 #####

用正则定位 `<p></p>` 标签位置，及所有属性，生成临时树结构HtmlTag，对象包含标签起始位置，属性和正文内容，供下一步加工使用

自己实现这种正则匹配需要很强的正则功底，正则不熟悉的同学请放弃，避免无谓浪费时间，最后实现出来不能用的代码

##### 方法二 第三方Html转对象 #####

目前C#、JAVA都有开源的Html转树对象代码仓库可以使用或参考，本人用CSQuery

优点，成熟的tokenize，完美匹配所有html标签书写不规范的情况，一句代码实现`CQ dom = CQ.Create(html);`，可以用nuget安装

缺点，暂时没有发现，如果出现解析错误的问题，可能后面的就没法用了

#### 递归遍历所有元素或位置，转换为HtmlNode树，提取有用信息，过滤不需要的信息 ####

先序遍历dom树结构，生成HtmlNode

```
private HtmlNode IterateIDOMElement(IDomObject element, HtmlNode parent)
{
    HtmlNode current;
    if (parent == null) //root节点
    {
        current = HtmlNode.CreateRoot();
    }
    else
    {
        if (element.NodeName == "#text")
        {
            current = HtmlNode.CreatePlainTag(element.ToString());
        }
        else
        {
			//这里可以增加解析代码，将部分信息精简提取出来
            var attrs = new StringBuilder();
            string link = null;
            if (element.HasAttributes)
            {
                element.Attributes.ToList().ForEach(x => attrs.AppendFormat("{0}='{1}' ", x.Key, x.Value));
                var href = element.Attributes.FirstOrDefault(x => x.Key.ToLower() == "href");
                link = href.Value;
            }
            current = HtmlNode.Create(attrs.ToString(), NameToTagType(element.NodeName));
            current.link = link;
        }
        parent.AddChild(current);
    }
    //遍历子树
    if (element.HasChildren)
    {
        for (var i = 0; i < element.ChildNodes.Count; i++)
        {
            IterateIDOMElement(element[i], current);//递归创建子节点
        }
    }
    return current;
}
```

NameToTagType方法负责封装将标签转换为什么类型的标签（换行、默认文本、图片等）

```
public enum TagType
{
    Breaking,
    Image,
    Default
}
private static TagType NameToTagType(string tagname) 
{
    if(string.IsNullOrEmpty(tagname)) return TagType.Default;
    switch (tagname.ToLower().Trim())
    {
        case "p":
        case "br":
        {
            return TagType.Breaking;
        }
        case "img":
        {
            return TagType.Image;
            }
        default:
            return TagType.Default;
    }
}
```

HtmlNode节点属性说明

```
public class HtmlNode
{
    private TagType type { get; set; }//节点类型
    private string properties { get; set; }//节点属性
    private string content { get; set; }//节点正文内容（纯文本）
	//所有子节点，如果本节点既有正文又有子节点，则应该把文本转换成节点，否则就损失了先后顺序
    private readonly List<HtmlNode> ChildTags = new List<HtmlNode>();
    private string _link;//保存链接
    public stirng link{
        private get
        {
			//...
		}
		set { _link = value; }
    }
	private string src//保存图片URL
    {
        get
        {
			//...
		}
	}
}
```

HtmlNode内置的创建帮助方法

```
public static HtmlNode Create(string properties, TagType type, string content = null){
    return new HtmlNode()
    {
        type = type,
        properties = properties,
        content = content
    };
}
public static HtmlNode CreateRoot(){
    return Create(null, TagType.Default);
}
public static HtmlNode CreatePlainTag(string text){
    return Create(null, TagType.Default, text);
}
public void AddChild(HtmlNode child){
    if (child == null) return;
    if (child.type == TagType.Default && !string.IsNullOrEmpty(child.content))
        child.content = child.content.Replace("&nbsp;", " ");
    ChildTags.Add(child);
}
```

### 2 后序遍历HtmlNode树，翻译各个节点，并合并生成List<ClientContentItem> ###

#### 第一步 完成后会得到一个根节点，后续遍历根节点，转换为List<ClientContentItem>列表 ####

`List<ClientContentItem> returnList = mockHtmlTag.BackOrderTravel();//对根节点进行后序遍历`

#### 第二步 后序遍历，先访问添加所有叶节点到数组，最后访问添加本节点 ####

BackOrderTravel方法

![](https://i.imgur.com/4DKSjmM.png)

如图，先把叶子节点转换为List对象，上层负责处理直接下一层的list对象和本层的对象，返回给上一层

每一次递归的逻辑：

1、如果**有子节点**，则对子节点进行后序遍历生成list返回
2、如果**无子节点**则将本节点变为list对象返回
3、如果**既存在子节点，本节点也存在内容**，那将检查本节点是否可以和子节点list合并，可以合并的话进行合并；不可以合并的加到List后部
4、对于Html标签中的链接、样式，上层链接或样式会影响下层链接或样式，但是如果下层有同样的链接或样式，则保留下层链接或样式

```
List<ClientContentItem> reList = new List<ClientContentItem>();
//后续遍历
if (ChildTags != null && ChildTags.Count > 0)
    foreach (var r in ChildTags) 
        AddClientContentItems(ref reList, r.BackOrderTravel(),this.link);
//访问根节点
ClientContentItem item = TransCurrentNodeToClientContentItem();
//加入本节点
if (item != null)
{
    if (reList.Count > 0)
    {
        var lastitem = reList.Last();
        if (lastitem.JoinAbleWith(item))
        {
            lastitem.JoinWith(item);
        }
        else
        {
            reList.Add(item);
        }
    }
    else
    {
        reList.Add(item);
    }        
}
return reList;
```

AddClientContentItems方法：将子节点生成的List依序加入整体List

```
private void AddClientContentItems(ref List<ClientContentItem> reList, List<ClientContentItem> backOrderTravel,string olink)
{
    if (backOrderTravel == null || backOrderTravel.Count == 0) return;
    //上层Link影响下层内容
    if (!string.IsNullOrEmpty(olink))
    {
        backOrderTravel.Where(r=>r.TextType == ClientTextType.Text).ToList().ForEach(x=>
        {
            x.TextType = ClientTextType.Href;
            x.Link = olink;
        });
        backOrderTravel.Where(r=>r.TextType == ClientTextType.Photo).ToList().ForEach(x=>x.Link = olink);
    }

    if (reList == null || reList.Count == 0)
    {
        reList = backOrderTravel;
        return;
    }
	//将两个list进行合并，如果有可以合并的item元素，则进行合并
    var lastitem = reList.Last();
    foreach (ClientContentItem curItem in backOrderTravel)
    {
        if (lastitem.JoinAbleWith(curItem))
        {
            lastitem.JoinWith(curItem);
        }
        else
        {
            lastitem = curItem;
            reList.Add(lastitem);
        }
    }
}
```

TransCurrentNodeToClientContentItem方法：将本节点转为listItem

```
private ClientContentItem TransCurrentNodeToClientContentItem()
{
    if (string.IsNullOrEmpty(content) && string.IsNullOrEmpty(link) && this.type == TagType.Default)
        return null;
    var currentItem = new ClientContentItem();
    if (this.type == TagType.Image)
    {
        currentItem.Text = src;//TODO：解析onclick和src
        currentItem.TextType = ClientTextType.Photo;
        if (!string.IsNullOrEmpty(link)) currentItem.Link = link;
    }
    else if (this.type == TagType.Breaking)
    {
        if (!string.IsNullOrEmpty(content))
            currentItem.Text = content;
        else
            currentItem.Text = string.Empty;
        currentItem.Text += "\r\n";
        currentItem.TextType = ClientTextType.Text;
        if (!string.IsNullOrEmpty(link))
        {
            currentItem.Link = link;
            currentItem.TextType = ClientTextType.Href;
        }
    }
    else if (this.type == TagType.Default && !string.IsNullOrEmpty(link))
    {
        if (!string.IsNullOrEmpty(content))
            currentItem.Text = content;
        currentItem.Link = link;
        currentItem.TextType = ClientTextType.Href;
    }
    else
    {
        if (!string.IsNullOrEmpty(content))
            currentItem.Text = content;
        currentItem.TextType = ClientTextType.Text;
    }
    return currentItem;
}
```

JoinAbleWith方法：检查两个ListItem是否可以合并，比如链接相同、或者没有链接的文本可以合并为一个元素

```
public bool JoinAbleWith(ClientContentItem curItem)
{
    if (this.TextType == curItem.TextType)
    {
        switch (this.TextType)
        {
            case ClientTextType.Href:
            {
                return this.Link == curItem.Link;
            }
            case ClientTextType.Text:
            {
                return true;
            }
        }
    }
    return false;
}
```

JoinWith方法：合并两个ListItem

```
public void JoinWith(ClientContentItem curItem)
{
    if (this.TextType == curItem.TextType)
    {
        switch (this.TextType)
        {
            case ClientTextType.Href:
            {
                if (this.Link == curItem.Link)
                {
                    this.Text = (this.Text ?? String.Empty) + curItem.Text;
                }
                break;
            }
            case ClientTextType.Text:
            {
                this.Text = (this.Text ?? String.Empty) + curItem.Text;
                break;
            }
        }
    }
}
```

### 3 最后加工 ###

现在已经生成好List对象了，可以对生成好的list对象进行再处理，完成某些兼容操作

例如，一般富文本维护的时候都会套一个P标签，P标签的尾部是换行符\r\n，这个时候可以把最末尾的换行符都去掉，以免影响展示

```
//List中的特殊处理
if (returnList != null && returnList.Count > 0)
{
    //去掉最后多余的换行符
    while (returnList.Count > 0 && !string.IsNullOrEmpty(returnList.Last().Text) && returnList.Last().Text.EndsWith("\r\n"))
    {
        returnList.Last().Text = returnList.Last().Text.Substring(0, returnList.Last().Text.LastIndexOf("\r\n", StringComparison.Ordinal));
        if (string.IsNullOrEmpty(returnList.Last().Text) && returnList.Last().TextType == ClientTextType.Text)
        {
            returnList.RemoveAt(returnList.Count - 1);
        }
    }
    for (int i = 0; i < returnList.Count - 1; i++)
    {
        if (returnList[i].TextType == ClientTextType.Photo &&
            returnList[i + 1].TextType == ClientTextType.Href &&
            !string.IsNullOrEmpty(returnList[i + 1].Link) &&
            string.IsNullOrEmpty(returnList[i + 1].Text))
        {
            returnList[i].Link = returnList[i + 1].Link;
        }
    }
    //过滤邮件和电话链接，产生新的Type
    foreach (var clientOntentItem in returnList.Where(r => r.TextType == ClientTextType.Href && !string.IsNullOrEmpty(r.Link)))
    {
        if (clientOntentItem.Link.ToLower().Contains("mailto:"))
        {
            clientOntentItem.Link = string.Empty;
            clientOntentItem.TextType = ClientTextType.Email;
        }
        if (clientOntentItem.Link.ToLower().Contains("telto:"))
        {
            clientOntentItem.Link = string.Empty;
            clientOntentItem.TextType = ClientTextType.Tel;
        }
    }
}
```

## JAVA 迁移 ##

CsQuery 使用的HtmlParser是从Java迁移过来的 The Validator.nu HTML Parser

除此之外在maven上还有HTML Parser Jar和Jericho HTML Parser

Java在做树解析的时候可以参考引用

jsoup和CSQuery最相近：

```
String html = "您好，您可以点击 <a lizard-catch=\"off\" onclick=\"chatUrlJumpLib.Jump('http://123123123', 2)\" >这里</a> ssssssss。";
Document doc = Jsoup.parse(html);
Elements body = doc.select("body");
```

[Java源代码参考](https://github.com/engimaxp/htmlparsertest)
[C#源代码参考](https://github.com/engimaxp/HtmlTranslateTest)
