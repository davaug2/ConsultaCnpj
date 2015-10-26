namespace WebApp.Models
{
    public class RfbCnpjCpfDataContract
    {
        public string RazaoSocial { get; set; }
        public string NomeFantasia { get; set; }
        public string Logradouro { get; set; }
        public string Numero { get; set; }
        public string Complemento { get; set; }
        public string Cep { get; set; }
        public string Bairro { get; set; }
        public string Cidade { get; set; }
        public string Estado { get; set; }
        public string CnaePrincipal { get; set; }
        public string CnaeSecundario { get; set; }
        public string NaturezaJuridica { get; set; }
        public bool Status { get; set; }
    }
}