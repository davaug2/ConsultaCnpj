#region

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using WebApp.Models;

#endregion

namespace WebApp.Controllers
{
    public class UtilsController : Controller
    {
        private const string UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-GB; rv:1.9.2.12) Gecko/20101026 Firefox/3.6.12";

        private const string AddressBase = "http://www.receita.fazenda.gov.br";
        private const string AddressCaptchaCNPJ = "/pessoajuridica/cnpj/cnpjreva/cnpjreva_solicitacao2.asp";
        private const string AddressDataCNPJ = "/pessoajuridica/cnpj/cnpjreva/valida.asp";
        private const string AddressCaptchaCPF = "/Aplicacoes/ATCTA/CPF/ConsultaPublica.asp";
        private const string AddressDataCPF = "/Aplicacoes/ATCTA/CPF/ConsultaPublicaExibir.asp";

        private static readonly string[] keysCNPJ =
        {
            "NOME EMPRESARIAL",
            "TITULO DO ESTABELECIMENTO (NOME DE FANTASIA)",
            "LOGRADOURO",
            "NUMERO",
            "COMPLEMENTO",
            "CEP",
            "BAIRRO/DISTRITO",
            "MUNICIPIO",
            "UF",
            "SITUACAO CADASTRAL",
            "CODIGO E DESCRICAO DA ATIVIDADE ECONOMICA PRINCIPAL",
            "CODIGO E DESCRICAO DAS ATIVIDADES ECONOMICAS SECUNDARIAS",
            "CODIGO E DESCRICAO DA NATUREZA JURIDICA"
        };

        public string InternalRFBGetCaptchaCPF()
        {
            try
            {
                var wc = new CookieAwareWebClient();
                wc.Headers["user-agent"] = UserAgent;
                var arrBytes = wc.DownloadData(AddressBase + AddressCaptchaCPF);
                var x = new HtmlDocument();
                x.LoadHtml(Encoding.UTF8.GetString(arrBytes));
                var image = x.GetElementbyId("imgCaptcha").GetAttributeValue("src", "");
                image = "http://www.receita.fazenda.gov.br/Aplicacoes/ATCTA/CPF/" +
                        image.Replace("&amp;", "&").Replace("./", "");
                var bytes = wc.DownloadData(image);
                var filename = Guid.NewGuid() + ".jpg";
                var path = Server.MapPath("~\\temp\\" + filename);
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                System.IO.File.WriteAllBytes(path, bytes);
                System.Web.HttpContext.Current.Session["GetCPFCookies"] = wc.CookieContainer;

                return VirtualPathUtility.ToAbsolute("~/temp/" + filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public string InternalRFBGetCaptchaCNPJ()
        {
            try
            {
                var wc = new CookieAwareWebClient();
                wc.Headers["user-agent"] = UserAgent;
                var arrBytes = wc.DownloadData(AddressBase + AddressCaptchaCNPJ);
                var x = new HtmlDocument();
                x.LoadHtml(Encoding.UTF8.GetString(arrBytes));
                var image = x.GetElementbyId("imgcaptcha").GetAttributeValue("src", "");
                var viewstate = (string) null; // x.GetElementbyId("viewstate").GetAttributeValue("value", "");
                image = AddressBase + "/pessoajuridica/cnpj/cnpjreva/" + image.Replace("&amp;", "&");
                var bytes = wc.DownloadData(image);
                var filename = Guid.NewGuid() + ".jpg";
                var path = Server.MapPath("~\\temp\\" + filename);
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                System.IO.File.WriteAllBytes(path, bytes);
                Session["GetCNPJViewState"] = viewstate;
                Session["GetCNPJCookies"] = wc.CookieContainer;

                return VirtualPathUtility.ToAbsolute("~/temp/" + filename);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool InternalRFBGetDataCNPJ(string cnpj, string captcha, out RfbCnpjCpfDataContract cnpjCpfData,
            out string mensagem)
        {
            var ret = new RfbCnpjCpfDataContract();
            try
            {
                mensagem = null;

                var sendData = new NameValueCollection();
                sendData.Add("cnpj", cnpj);
                sendData.Add("txtTexto_captcha_serpro_gov_br", captcha);
                sendData.Add("search_type", "cnpj");
                sendData.Add("origem", "comprovante");
                sendData.Add("submit1", "Consultar");
                //sendData.Add("viewstate", HttpContext.Current.Session["GetCNPJViewState"].ToString());

                var wc = new CookieAwareWebClient();
                wc.CookieContainer = Session["GetCNPJCookies"] as CookieContainer;
                wc.CookieContainer.SetCookies(new Uri(AddressBase), "flag=1");
                wc.Headers.Add("User-Agent", UserAgent);
                var resultByte = wc.UploadValues(AddressBase + AddressDataCNPJ, "POST", sendData);

                var result = Encoding.Default.GetString(resultByte);

                var valid = new HtmlDocument();
                valid.LoadHtml(result);
                if (valid.GetElementbyId("imgcaptcha") != null)
                    throw new ArgumentException(@"Captcha inválido", nameof(captcha));

                if (result.Contains("lido. Verifique se o mesmo foi digitado corretamente"))
                    throw new ArgumentException(@"Cnpj inválido", nameof(UtilsController));

                if (result.Contains("mero de CNPJ informado. Verifique se o mesmo foi digitado corretamente."))
                    throw new ArgumentException(@"Cnpj inválido", nameof(UtilsController));

                result = result.Remove(0, result.IndexOf("NOME EMPRESARIAL"));
                result = result.Remove(result.IndexOf("DATA DA SITUA"), result.Length - result.IndexOf("DATA DA SITUA"));
                var docHtml = new HtmlDocument();
                docHtml.LoadHtml(result);
                var dataResult =
                    docHtml.DocumentNode.Descendants()
                        .Where(i => i.Name == "font")
                        .Select(i => i.InnerText.Replace("\t", "").Replace("\r", "").Replace("\n", "").RemoveAccents())
                        .ToList();
                var available = string.Empty;

                ret.Status = true;
                foreach (var item in dataResult)
                {
                    if (!string.IsNullOrEmpty(available))
                    {
                        var data = CultureInfo.GetCultureInfo("pt-BR").TextInfo.ToTitleCase(item.Trim());
                        switch (available)
                        {
                            case "NOME EMPRESARIAL":
                                ret.RazaoSocial = data;
                                break;
                            case "TITULO DO ESTABELECIMENTO (NOME DE FANTASIA)":
                                ret.NomeFantasia = data;
                                break;
                            case "LOGRADOURO":
                                ret.Logradouro = data;
                                break;
                            case "NUMERO":
                                ret.Numero = data;
                                break;
                            case "COMPLEMENTO":
                                ret.Complemento = data;
                                break;
                            case "CEP":
                                ret.Cep = data;
                                break;
                            case "BAIRRO/DISTRITO":
                                ret.Bairro = data;
                                break;
                            case "MUNICIPIO":
                                ret.Cidade = data;
                                break;
                            case "UF":
                                ret.Estado = data;
                                break;
                            case "SITUACAO CADASTRAL":
                                ret.Status = data.Trim().ToLower() == "ativa";
                                break;
                            case "CODIGO E DESCRICAO DA ATIVIDADE ECONOMICA PRINCIPAL":
                                ret.CnaePrincipal = data;
                                break;
                            case "CODIGO E DESCRICAO DAS ATIVIDADES ECONOMICAS SECUNDARIAS":
                                ret.CnaeSecundario = data;
                                break;
                            case "CODIGO E DESCRICAO DA NATUREZA JURIDICA":
                                ret.NaturezaJuridica = data;
                                break;
                        }
                        available = null;
                    }
                    if (keysCNPJ.Contains(item))
                        available = item;
                }
                cnpjCpfData = ret;
                return true;
            }
            catch (Exception)
            {
                mensagem = "Captcha inválido";
            }
            cnpjCpfData = null;
            return false;
        }

        public bool InternalRFBGetData(string document, string captcha, out RfbCnpjCpfDataContract saida,
            out string mensagem)
        {
            document = StringHelper.GetStringNumbersOnly(document);
            if (document.Length == 14)
                return InternalRFBGetDataCNPJ(document, captcha, out saida, out mensagem);
            saida = null;
            mensagem = "Documento inválido";
            return false;
        }

        public string InternalRFBGetCaptcha(string document)
        {
            document = StringHelper.GetStringNumbersOnly(document);
            if (document.Length == 14)
                return InternalRFBGetCaptchaCNPJ();
            if (document.Length == 11)
                return InternalRFBGetCaptchaCPF();
            return null;
        }

        public JsonResult RFBGetCaptcha(string document)
        {
            var img = InternalRFBGetCaptcha(document);
            return Json(new {img}, JsonRequestBehavior.AllowGet);
        }


        public JsonResult RFBGetData(string cnpj, string captcha)
        {
            RfbCnpjCpfDataContract listResult;
            string mensagem;
            var ret = InternalRFBGetData(cnpj, captcha, out listResult, out mensagem);
            return Json(new {ret, mensagem, dados = listResult}, JsonRequestBehavior.AllowGet);
        }

        public void DeteleFile(string patch)
        {
            var fi = new FileInfo(Server.MapPath(patch));
            fi.Delete();
        }
    }
}