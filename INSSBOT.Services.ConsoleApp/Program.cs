using INSSBOT.Domain.Model;
using System;
using System.Collections.Generic;
using Telegram.Bot.Types;
using System.Linq;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;
using System.Globalization;
using INSSBOT.Application;

namespace INSSBOT.Services.ConsoleApp
{
    class Program
    {
        public static Dictionary<long, Usuario> UsuarioTelegram = new Dictionary<long, Usuario>();

        static void Main(string[] args)
        {
            Bot.Api.OnMessage += Bot_OnMessage;
            Bot.Api.OnMessageEdited += Bot_OnMessage;

            Bot.Api.StartReceiving();

            Console.WriteLine("Serviço iniciado!");

            Console.ReadLine();

            Bot.Api.StopReceiving();
        }

        private static void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.TextMessage)
            {
                Usuario user;
                Passo proximoPasso = new Passo();
                bool respostaValida = true;
                if (!UsuarioTelegram.ContainsKey(e.Message.From.Id))
                {
                    user = new Usuario();
                    user.UsuarioTelegram = e.Message.From;
                    user.UsuarioChat = e.Message.Chat;
                    proximoPasso = user.Passos.FirstOrDefault(x => x.Ordem == 1);
                    user.PassoAtual = proximoPasso;
                    UsuarioTelegram.Add(e.Message.From.Id, user);
                }
                else
                {
                    user = UsuarioTelegram[e.Message.From.Id];
                    respostaValida = ArmazenarResposta(user, e.Message.Text.ToUpper());
                    proximoPasso = user.Passos.FirstOrDefault(x => x.Ordem == user.PassoAtual.Ordem + 1);
                }

                if (proximoPasso != null && respostaValida)
                {
                    user.PassoAtual = proximoPasso;
                    EnviarMensagem(user, user.PassoAtual.Descricao);
                }
                else
                {
                    if (respostaValida)
                    {
                        user.PassoAtual = user.Passos.FirstOrDefault(x => x.Ordem == 0);
                        CalcularAposentadoria cal = new CalcularAposentadoria();
                        Bot.Api.SendTextMessageAsync(user.UsuarioChat.Id, cal.CalcularTempo(user), replyMarkup: new ReplyKeyboardRemove());
                        Bot.Api.SendTextMessageAsync(user.UsuarioChat.Id, $"Fim", replyMarkup: new ReplyKeyboardRemove());
                    }
                    else
                    {
                        EnviarMensagem(user, "Resposta inváida");
                    }
                }
            }
        }

        private static void EnviarMensagem(Usuario user, string mensagem)
        {
            if (user.PassoAtual.Opcoes)
            {
                Bot.Api.SendTextMessageAsync(user.UsuarioChat.Id, mensagem, replyMarkup: user.PassoAtual.DescricaoBooleana);
            }
            else
            {
                Bot.Api.SendTextMessageAsync(user.UsuarioChat.Id, mensagem, replyMarkup: new ReplyKeyboardRemove());
            }
        }

        private static bool ArmazenarResposta(Usuario user, string Mensagem)
        {
            int auxi = 0;
            DateTime aux;
            switch (user.PassoAtual.Ordem)
            {
                case 1:
                    user.Contribuinte.Nome = Mensagem;
                    break;
                case 2:
                    if (DateTime.TryParse(Mensagem, out aux))
                    {
                        int anos = DateTime.Now.Year - aux.Year;
                        if (DateTime.Now.Month < aux.Month || (DateTime.Now.Month == aux.Month && DateTime.Now.Day < aux.Day))
                            anos--;
                        user.Contribuinte.Idade = anos;
                        user.Contribuinte.DataNascimento = Mensagem;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 3:
                    user.Contribuinte.CPF = Mensagem;
                    break;
                case 4:
                    if (Mensagem == "MASCULINO" || Mensagem == "FEMININO")
                    {
                        user.Contribuinte.Sexo = Mensagem;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 5:
                    user.Contribuinte.Email = Mensagem;
                    break;
                case 6:
                    user.Contribuinte.Celular = Mensagem;
                    break;
                case 7:
                    user.Contribuinte.Endereco = Mensagem;
                    break;
                case 8:
                    if (Mensagem == "SIM" || Mensagem == "NÃO")
                    {
                        user.Contribuinte.FuncionarioRural = Mensagem == "SIM" ? true : false;
                        if (Mensagem == "SIM")
                        {
                            user.PassoAtual = user.Passos.FirstOrDefault(x => x.Ordem == user.PassoAtual.Ordem + 3);
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 9:
                    if (Mensagem == "SIM" || Mensagem == "NÃO")
                    {
                        user.Contribuinte.FuncionarioPublico = Mensagem == "SIM" ? true : false;
                        if (Mensagem == "SIM")
                        {
                            user.PassoAtual = user.Passos.FirstOrDefault(x => x.Ordem == user.PassoAtual.Ordem + 2);
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 10:
                    if (Mensagem == "SIM" || Mensagem == "NÃO")
                    {
                        user.Contribuinte.RecebeuInsalubridade = Mensagem == "SIM" ? true : false;
                        if (Mensagem == "NÃO")
                        {
                            user.PassoAtual = user.Passos.FirstOrDefault(x => x.Ordem == user.PassoAtual.Ordem + 1);
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 11:
                    if (int.TryParse(Mensagem, out auxi))
                    {
                        user.Contribuinte.TempoInsalubridade = int.Parse(Mensagem);
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case 12:
                    if (int.TryParse(Mensagem, out auxi))
                    {
                        user.Contribuinte.TempoContribuicao = int.Parse(Mensagem);
                    }
                    else
                    {
                        return false;
                    }
                    break;
                default:
                    break;
            }
            return true;
        }
    }
}
