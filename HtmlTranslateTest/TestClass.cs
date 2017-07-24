using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using NUnit.Framework;
using CsQuery;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.HtmlParser;

namespace HtmlTranslateTest
{
    [TestFixture]
    public class TestClass
    {
        
        [Test]
        public void TestCSQuery() {
            string mock = "<p></p>";
            var b = CreateDOMTree(mock);
            Assert.IsNotNull(b);

            var a = TransCodeHtmlToListV2(mock);
            Assert.IsNotNull(a);
        }
        public HtmlNode CreateDOMTree(string html) {
            HtmlNode root = HtmlNode.CreateRoot();
            //无内容
            if (string.IsNullOrEmpty(html))
                return root;
            CQ dom = CQ.Create(html);
            //再以此处理各个位置的关系
            root = IterateIDOMElement(dom.Document,null);
            return root;
        }
        public HtmlNode IterateIDOMElement(IDomObject element, HtmlNode parent) {
            HtmlNode current;
            if (parent == null)//root节点
            {
                current = HtmlNode.CreateRoot();
            }
            else {
                if (element.NodeName == "#text")
                {
                    current = HtmlNode.CreatePlainTag(element.ToString());
                }
                else {
                    StringBuilder attrs = new StringBuilder();
                    string link = null;
                    if (element.HasAttributes) {
                        element.Attributes.ToList().ForEach(x => attrs.AppendFormat("{0}='{1}'", x.Key, x.Value));
                        var href = element.Attributes.FirstOrDefault(x => x.Key.ToLower() == "href");
                        link = href.Value;
                    }
                    current = HtmlNode.Create(attrs.ToString(), NameToTagType(element.NodeName));
                    current.link = link;
                }
                parent.AddChild(current);
            }
            //遍历子树
            if (element.HasChildren) {
                for (int i = 0; i < element.ChildNodes.Count; i++) {
                    IterateIDOMElement(element[i], current);
                }
            }
            return current;
        }
        
        private string TransCodeHtmlToListV2(string html)
        {
            //htmlToTree
            var mockHtmlTag = CreateDOMTree(html);
            //TreeToList
            List<ClientContentItem> returnList = mockHtmlTag.BackOrderTravel();

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
            return JsonConvert.SerializeObject(returnList);
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

        private static List<HtmlNode> CreateNodesFromTag(List<HtmlTag> tags, int targetStartPos,string html, int tagLevel)
        {
            if (string.IsNullOrEmpty(html)) return null;
            List<HtmlNode> result = new List<HtmlNode>();
            int currentPos = 0;
            var currentProcessingTag = tags.Where(x => x.tagLevel == tagLevel).ToList();
            foreach (var tag in currentProcessingTag)
            {
                result.Add(HtmlNode.CreatePlainTag(html.Substring(currentPos, tag.tagStartPos - currentPos - targetStartPos)));
                var currentNode = HtmlNode.Create(tag.properties, NameToTagType(tag.tagname));
                currentNode.AddChilds(CreateNodesFromTag(tags.Where(r => r.tagStartPos <= tag.tagContentEndPos && r.tagStartPos >= tag.tagContentStartPos).ToList(), tag.tagContentStartPos, html.Substring(tag.tagContentStartPos - targetStartPos, tag.tagContentEndPos - tag.tagContentStartPos), tagLevel + 1));
                result.Add(currentNode);
                currentPos = tag.tagEndPos - targetStartPos;
            }
            result.Add(HtmlNode.CreatePlainTag(html.Substring(currentPos,html.Length-currentPos)));
            return result;
        }
        
    }

    public class HtmlTag
    {
        public string properties { get; set; }
        public int tagStartPos { get; set; }
        public int tagContentStartPos { get; set; }
        public int tagContentEndPos { get; set; }
        public int tagEndPos { get; set; }
        public string tagname { get; set; }
        public int tagLevel { get; set; }

        public override string ToString()
        {
            return string.Format("{{{0}, {1}, {2}, {3}, {4}, {5}, {6}}}", properties, tagStartPos, tagContentStartPos, tagContentEndPos, tagEndPos, tagname, tagLevel);
        }
    }

    public enum TagType
    {
        Breaking,
        Image,
        Default
    }

    public class HtmlNode
    {
        private static readonly Regex regexFun = new Regex(@"(?i)\bchatUrlJumpLib.Jump\b\('(?<url>.*)\',(?<type>.*)\)");
        private static readonly Regex REGEX_URL = new Regex(@"(?i)((http|https)://)?(www.)?(([a-zA-Z0-9\._-]+\.[a-zA-Z]{2,6})|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,9})*(/[a-zA-Z0-9\&#,%_\./-~-]*)?");
        private static readonly Regex REGEX_HREF = new Regex(@"(?i)href=""\s*(.+?)\s*""");
        private static readonly Regex REGEX_PICTURE_URL = new Regex(@"(?i)src=['""](\S+)['""]\s");
        public static HtmlNode Create(string properties, TagType type, string content = null)
        {
            return new HtmlNode()
            {
                type = type,
                properties = properties,
                content = content
            };
        }
        public static HtmlNode CreateRoot()
        {
            return Create(null, TagType.Default);
        }
        public static HtmlNode CreatePlainTag(string text)
        {
            return Create(null, TagType.Default, text);
        }

        public TagType type { get; set; }
        public string properties { get; set; }
        public string content { get; set; }
        private readonly List<HtmlNode> ChildTags = new List<HtmlNode>();
        private string _link;
        public string link
        {
            get
            {
                if (!string.IsNullOrEmpty(_link)) return _link;
                if (string.IsNullOrEmpty(properties)) return string.Empty;
                MatchCollection mcFun = regexFun.Matches(properties);
                if (mcFun.Count > 0)
                {
                    _link = mcFun[0].Groups["url"].Value;
                    return _link;
                }
                mcFun = REGEX_HREF.Matches(properties);
                if(mcFun.Count > 0)
                {
                    _link = mcFun[0].Groups[1].Captures[0].Value;
                    return _link;
                }
                mcFun = REGEX_URL.Matches(properties);
                if (mcFun.Count > 0)
                {
                    _link = mcFun[0].Groups[0].Captures[0].Value;
                    return _link;
                }
                return string.Empty;
            }
            set { _link = value; }
        }
        public string src
        {
            get
            {
                if (string.IsNullOrEmpty(properties)) return string.Empty;
                MatchCollection mcFun = regexFun.Matches(properties);
                mcFun = REGEX_PICTURE_URL.Matches(properties);
                if (mcFun.Count > 0)
                {
                    return mcFun[0].Groups[1].Captures[0].Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
        public void AddChild(HtmlNode child)
        {
            if (child == null) return;
            if (child.type == TagType.Default && !string.IsNullOrEmpty(child.content))
                child.content = child.content.Replace("&nbsp;", " ");
            if (child.type == TagType.Default && string.IsNullOrEmpty(child.content) &&
                string.IsNullOrEmpty(child.properties))
                return;
            ChildTags.Add(child);
        }
        public void AddChilds(List<HtmlNode> childs)
        {
            if (childs == null || childs.Count == 0) return;
            if (childs.TrueForAll(x => x.IsPlainText()))
            {
                if (content == null) content = String.Empty;
                childs.ForEach(r=>content+=r.content);
            }
            else
            {
                childs.ForEach(AddChild);    
            }
        }

        public bool IsPlainText()
        {
            if (type == TagType.Default && string.IsNullOrEmpty(properties))
            {
                if (ChildTags == null || ChildTags.Count == 0) return true;
                return ChildTags.TrueForAll(r => r.IsPlainText());
            }
            return false;
        }

        public override string ToString()
        {
            return FormatIndexString(String.Empty);
        }

        private string FormatIndexString(string prefix)
        {
            StringBuilder stringBuilder = new StringBuilder(string.Format("{4}type: {0}, properties: {1}, content: {2}, link: {3}", type, properties, content, link,prefix));
            if (ChildTags != null && ChildTags.Count > 0)
            {
                ChildTags.ForEach(x => stringBuilder.Append("\r\n" + x.FormatIndexString(prefix+"\t")));
            }
            return stringBuilder.ToString();
        }

        protected bool Equals(HtmlNode other)
        {
            bool result = type == other.type && string.Equals(properties, other.properties) && string.Equals(content, other.content);
            if(result == false) return result;
            if (ChildTags == null && other.ChildTags == null) return true;
            if (ChildTags!=null && other.ChildTags!=null)
            {
                if (ChildTags.Count == 0 && other.ChildTags.Count == 0) return true;
                if (ChildTags.Count != other.ChildTags.Count) return false;
                return !ChildTags.Where((t, i) => !t.Equals(other.ChildTags[i])).Any();
            }
            return false;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HtmlNode) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) type;
                hashCode = (hashCode*397) ^ (properties != null ? properties.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (content != null ? content.GetHashCode() : 0);
                return hashCode;
            }
        }

        public List<ClientContentItem> BackOrderTravel()
        {
            List<ClientContentItem> reList = new List<ClientContentItem>();
            //后续遍历
            if (ChildTags != null && ChildTags.Count > 0)
                foreach (var r in ChildTags) 
                    AddClientContentItems(ref reList, r.BackOrderTravel(),this.link);
            
            //访问根节点
            ClientContentItem item = TransCurrentNodeToClientContentItem();

            //加入根节点
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
        }

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

        /// <summary>
        /// 合并List
        /// </summary>
        /// <param name="reList"></param>
        /// <param name="backOrderTravel"></param>
        /// <param name="olink">上层链接</param>
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
    }
    
    public class ClientContentItem
    {
        public ClientContentItem()
        {
            Text = string.Empty;
            Link = string.Empty;
            TextType = ClientTextType.Text;
        }

        public ClientTextType TextType { get; set; }
        public string Text { get; set; }
        public string Link { get; set; }

        public bool isEmpty()
        {
            return string.IsNullOrEmpty(Text);
        }

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
    }

    public enum ClientTextType
    {
        /// <summary>
        /// 文本类型
        /// </summary>
        Text = 1,

        /// <summary>
        /// 超链
        /// </summary>
        Href = 2,

        /// <summary>
        /// 表情
        /// </summary>
        Expression = 3,

        /// <summary>
        /// 图片
        /// </summary>
        Photo = 4,

        /// <summary>
        /// 电话
        /// </summary>
        Tel = 5,

        /// <summary>
        /// 邮件
        /// </summary>
        Email = 6
    }
}
