using INSSBOT.Domain.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace INSSBOT.Application
{
    public class CalcularAposentadoria
    {
        public string CalcularTempo(Usuario usuario)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("RESUMO DAS INFORMAÇÕES");
            sb.AppendLine("Nome Completo: " + usuario.Contribuinte.Nome);
            sb.AppendLine("Data de Nascimento: " + usuario.Contribuinte.DataNascimento);
            sb.AppendLine("CPF: " + usuario.Contribuinte.CPF);
            sb.AppendLine("Sexo: " + usuario.Contribuinte.Sexo);
            sb.AppendLine("E-mail: " + usuario.Contribuinte.Email);
            sb.AppendLine("Celular: " + usuario.Contribuinte.Celular);
            sb.AppendLine("Endereço completo: " + usuario.Contribuinte.Endereco);
            sb.AppendLine("Funcionário rural: " + (usuario.Contribuinte.FuncionarioRural ? "Sim" : "Não"));
            sb.AppendLine("Funcionário público: " + (usuario.Contribuinte.FuncionarioPublico ? "Sim" : "Não"));
            sb.AppendLine("Recebeu insalubridade ou periculosidade: " + (usuario.Contribuinte.RecebeuInsalubridade ? "Sim" : "Não"));
            sb.AppendLine("Por quanto tempo (em meses): " + usuario.Contribuinte.TempoInsalubridade);
            sb.AppendLine("Por quanto tempo (em meses) contribuiu para o INSS: " + usuario.Contribuinte.TempoContribuicao);
            sb.AppendLine("**********************************");

            string resultadoRegraAtual = string.Empty;
            string resultadoRegraNova = string.Empty;
            int tempoContribuicao_Anos = (usuario.Contribuinte.TempoContribuicao / 12);


            if (usuario.Contribuinte.FuncionarioRural)
            {
                resultadoRegraAtual = CalcularTempoRural(usuario, tempoContribuicao_Anos, false);
                resultadoRegraNova = CalcularTempoRural(usuario, tempoContribuicao_Anos, true);
            }
            if (usuario.Contribuinte.FuncionarioPublico)
            {
                resultadoRegraAtual = CalcularTempoPublico(usuario, tempoContribuicao_Anos, false);
                resultadoRegraNova = CalcularTempoPublico(usuario, tempoContribuicao_Anos, true);
            }
            else if (!usuario.Contribuinte.FuncionarioPublico && !usuario.Contribuinte.FuncionarioRural)
            {
                resultadoRegraAtual = CalcularTempoPrivado(usuario, tempoContribuicao_Anos, false);
                resultadoRegraNova = CalcularTempoPrivado(usuario, tempoContribuicao_Anos, true);
            }

            sb.AppendLine(resultadoRegraAtual);
            sb.AppendLine("**********************************");
            sb.AppendLine(resultadoRegraNova);
            return sb.ToString();
        }

        private static string CalcularTempoPrivado(Usuario usuario, int tempoContribuicao_Anos, bool novaRegra)
        {
            float tempoContribuicaoInsalubridade = 0;
            string resultado = string.Empty;
            float idadeAposentadoria;
            float insalubridade;
            float tempContrmin = 0;
            float tempContrmax = 0;

            if (usuario.Contribuinte.Sexo == "MASCULINO")
            {
                insalubridade = (float)1.4;
                if (novaRegra)
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PrivadoIdadeMinimaMNova"]);
                    tempContrmin = int.Parse(ConfigurationSettings.AppSettings["PrivadoTempoContribuicaoMNova"]);
                    tempContrmax = int.Parse(ConfigurationSettings.AppSettings["PrivadoTempoContribuicaoMNovaMaximo"]);
                }
                else
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PrivadoTempoContribuicaoM"]);
                }
            }
            else
            {
                insalubridade = (float)1.2;
                if (novaRegra)
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PrivadoIdadeMinimaFNova"]);
                    tempContrmin = int.Parse(ConfigurationSettings.AppSettings["PrivadoTempoContribuicaoFNova"]);
                    tempContrmax = int.Parse(ConfigurationSettings.AppSettings["PrivadoTempoContribuicaoFNovaMaximo"]);
                }
                else
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PrivadoTempoContribuicaoF"]);
            }

            tempoContribuicaoInsalubridade = 0;
            if (usuario.Contribuinte.RecebeuInsalubridade)
            {
                var criarinsalubridade = (float)usuario.Contribuinte.TempoInsalubridade;
                tempoContribuicaoInsalubridade = (float)(criarinsalubridade * insalubridade) / 12;
            }

            if (novaRegra)
            {
                var totalTempoFaltaAposentarMin = tempContrmin - tempoContribuicao_Anos;
                var totalTempoFaltaAposentarMax = tempContrmax - tempoContribuicao_Anos;
                var totalIdadeFaltaAposentar = idadeAposentadoria - usuario.Contribuinte.Idade;
                totalTempoFaltaAposentarMin = totalTempoFaltaAposentarMin < 0 ? 0 : totalTempoFaltaAposentarMin;
                totalTempoFaltaAposentarMax = totalTempoFaltaAposentarMax < 0 ? 0 : totalTempoFaltaAposentarMax;
                totalIdadeFaltaAposentar = totalIdadeFaltaAposentar < 0 ? 0 : totalIdadeFaltaAposentar;

                if (tempoContribuicao_Anos >= tempContrmax && usuario.Contribuinte.Idade >= idadeAposentadoria)
                {
                    resultado = "Regra Nova: Parabéns... de acordo com a nova regra você já pode se aposentar com o valor integral do benefício.";
                }
                else if (tempoContribuicao_Anos >= tempContrmin && usuario.Contribuinte.Idade >= idadeAposentadoria)
                {
                    resultado = "Regra Nova: Parabéns... de acordo com a nova regra você já pode se aposentar com 60% do valor do benefício. Para aposentadoria com o valor integral ";
                    resultado += "faltam " + totalTempoFaltaAposentarMax + " anos de contribuição.";
                }
                else
                {
                    resultado += "Regra Nova: Faltam " + totalTempoFaltaAposentarMin + " anos de contribuição e mais " + totalIdadeFaltaAposentar + " anos de idade para sua aposentadoria com 60% do valor do benefício, para aposentadoria com o valor integral ";
                    resultado += "faltam " + totalTempoFaltaAposentarMax + " anos de contribuição.";
                }
            }
            else
            {
                float pontosTotais = (tempoContribuicao_Anos + tempoContribuicaoInsalubridade) + usuario.Contribuinte.Idade;
                if (pontosTotais >= idadeAposentadoria)
                {
                    resultado = "Regra Atual: Parabéns... de acordo com a regra atual você já pode se aposentar.";
                }
                else
                {
                    float tempoRestante = ((idadeAposentadoria - pontosTotais) / 2);
                    var values = tempoRestante.ToString(CultureInfo.InvariantCulture).Split('.');
                    if (!string.IsNullOrEmpty(values[0]) && values[0] != "0")
                    {
                        resultado += "Regra Atual: Faltam " + values[0] + " anos";
                        if (values.Length <= 1)
                        {
                            resultado += " para sua aposentadoria de acordo com a regra atual.";
                        }
                    }
                    if (values.Length > 1 && !string.IsNullOrEmpty(values[1]) && values[1] != "0")
                    {
                        resultado += " e " + values[1].Substring(0, 1) + " meses para sua aposentadoria de acordo com a regra atual.";
                    }
                }
            }

            return resultado;
        }

        private static string CalcularTempoPublico(Usuario usuario, int tempoContribuicao_Anos, bool novaRegra)
        {
            string resultado = string.Empty;
            float tempContrmax = 0;
            var tempContr = 0;
            float idadeAposentadoria = 0;

            if (usuario.Contribuinte.Sexo == "MASCULINO")
            {
                tempContrmax = int.Parse(ConfigurationSettings.AppSettings["PublicoTempoContribuicaoMNovaMaximo"]);

                if (novaRegra)
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PublicoIdadeMinimaMNova"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["PublicoTempoContribuicaoMNova"]);
                }
                else
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PublicoIdadeMinimaM"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["PublicoTempoContribuicaoM"]);
                }
            }
            else
            {
                tempContrmax = int.Parse(ConfigurationSettings.AppSettings["PublicoTempoContribuicaoFNovaMaximo"]);
                if (novaRegra)
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PublicoIdadeMinimaFNova"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["PublicoTempoContribuicaoFNova"]);
                }
                else
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["PublicoIdadeMinimaF"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["PublicoTempoContribuicaoF"]);
                }
            }

            if (novaRegra)
            {
                var totalTempoFaltaAposentarMin = tempContr - tempoContribuicao_Anos;
                var totalTempoFaltaAposentarMax = tempContrmax - tempoContribuicao_Anos;
                var totalIdadeFaltaAposentar = idadeAposentadoria - usuario.Contribuinte.Idade;
                totalTempoFaltaAposentarMin = totalTempoFaltaAposentarMin < 0 ? 0 : totalTempoFaltaAposentarMin;
                totalTempoFaltaAposentarMax = totalTempoFaltaAposentarMax < 0 ? 0 : totalTempoFaltaAposentarMax;
                totalIdadeFaltaAposentar = totalIdadeFaltaAposentar < 0 ? 0 : totalIdadeFaltaAposentar;

                if (tempoContribuicao_Anos >= tempContrmax && usuario.Contribuinte.Idade >= idadeAposentadoria)
                {
                    resultado = "Regra Nova: Parabéns... de acordo com a nova regra você já pode se aposentar com o valor integral do benefício.";
                }
                else if (tempoContribuicao_Anos >= tempContr && usuario.Contribuinte.Idade >= idadeAposentadoria)
                {
                    resultado = "Regra Nova: Parabéns... de acordo com a nova regra você já pode se aposentar com 70% do valor do benefício. Para aposentadoria com o valor integral ";
                    resultado += "faltam " + totalTempoFaltaAposentarMax + " anos de contribuição.";
                }
                else
                {
                    resultado += "Regra Nova: Faltam " + totalTempoFaltaAposentarMin + " anos de contribuição e mais " + totalIdadeFaltaAposentar + " anos de idade para sua aposentadoria com 60% do valor do benefício, para aposentadoria com o valor integral ";
                    resultado += "faltam " + totalTempoFaltaAposentarMax + " anos de contribuição.";
                }
            }
            else
            {
                if (tempoContribuicao_Anos >= tempContr && usuario.Contribuinte.Idade >= idadeAposentadoria)
                {
                    if (novaRegra)
                        resultado = "Regra Nova: Parabéns... de acordo com a nova regra você já pode se aposentar.";
                    else
                        resultado = "Regra Atual: Parabéns... de acordo com a regra atual você já pode se aposentar.";
                }
                else
                {
                    var totalTempoFaltaAposentar = tempContr - tempoContribuicao_Anos;
                    var totalIdadeFaltaAposentar = idadeAposentadoria - usuario.Contribuinte.Idade;
                    totalTempoFaltaAposentar = totalTempoFaltaAposentar < 0 ? 0 : totalTempoFaltaAposentar;
                    totalIdadeFaltaAposentar = totalIdadeFaltaAposentar < 0 ? 0 : totalIdadeFaltaAposentar;

                    if (novaRegra)
                        resultado += "Regra Nova: Faltam " + totalTempoFaltaAposentar + " anos de contribuição e mais " + totalIdadeFaltaAposentar + " anos de idade para sua aposentadoria de acordo com a nova regra.";
                    else
                        resultado += "Regra Atual: Faltam " + totalTempoFaltaAposentar + " anos de contribuição e mais " + totalIdadeFaltaAposentar + " anos de idade para sua aposentadoria de acordo com a regra atual.";
                }
            }
            return resultado;
        }

        private static string CalcularTempoRural(Usuario usuario, int tempoContribuicao_Anos, bool novaRegra)
        {
            string resultado = string.Empty;
            var tempContr = 0;
            float idadeAposentadoria = 0;

            if (usuario.Contribuinte.Sexo == "MASCULINO")
            {
                if (novaRegra)
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["RuralIdadeMinimaMNova"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["RuralTempoContribuicaoMNova"]);
                }
                else
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["RuralIdadeMinimaM"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["RuralTempoContribuicaoM"]);
                }
            }
            else
            {
                if (novaRegra)
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["RuralIdadeMinimaFNova"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["RuralTempoContribuicaoFNova"]);
                }
                else
                {
                    idadeAposentadoria = int.Parse(ConfigurationSettings.AppSettings["RuralIdadeMinimaF"]);
                    tempContr = int.Parse(ConfigurationSettings.AppSettings["RuralTempoContribuicaoF"]);
                }
            }

            if (tempoContribuicao_Anos >= tempContr && usuario.Contribuinte.Idade >= idadeAposentadoria)
            {
                if (novaRegra)
                    resultado = "Regra Nova: Parabéns... de acordo com a nova regra você já pode se aposentar.";
                else
                    resultado = "Regra Atual: Parabéns... de acordo com a regra atual você já pode se aposentar.";
            }
            else
            {
                var totalTempoFaltaAposentar = tempContr - tempoContribuicao_Anos;
                var totalIdadeFaltaAposentar = idadeAposentadoria - usuario.Contribuinte.Idade;

                totalTempoFaltaAposentar = totalTempoFaltaAposentar < 0 ? 0 : totalTempoFaltaAposentar;
                totalIdadeFaltaAposentar = totalIdadeFaltaAposentar < 0 ? 0 : totalIdadeFaltaAposentar;

                if (novaRegra)
                    resultado += "Regra Nova: Faltam " + totalTempoFaltaAposentar + " anos de contribuição e mais " + totalIdadeFaltaAposentar + " anos de idade para sua aposentadoria de acordo com a nova regra.";
                else
                    resultado += "Regra Atual: Faltam " + totalTempoFaltaAposentar + " anos de contribuição e mais " + totalIdadeFaltaAposentar + " anos de idade para sua aposentadoria de acordo com a regra atual.";
            }

            return resultado;
        }
    }
}
