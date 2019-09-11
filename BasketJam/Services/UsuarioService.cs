﻿using BasketJam.Helper;
using BasketJam.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using WebApi.Helpers;

namespace BasketJam.Services
{
    public interface IUsuarioService
    {
        Task<Usuario> Autenticar(string nomUser, string password);
        //List<Usuario> GetAll();
        Task<Object> Create(Usuario usuario);
        Task<Usuario> Get(string id);

        Task<List<Usuario>> Get();

        bool BuscarUsuarioPorCI(string ci);
        Task<Object> VerificarCuenta(string activationCode);
        void SendPassReset(string emailId);
        Task<Boolean> CambiarPassword(string email, string password);
        //IEnumerable<Usuario> GetAll();
    }

    public class UsuarioService : IUsuarioService
    {

        private readonly IMongoCollection<Usuario> _usuarios;

        private readonly AppSettings _appSettings;

        private IConfiguracionUsuarioMovilService _configuracionUsuarioMovilService;


        string coso;

        public UsuarioService(IOptions<AppSettings> appSettings, IConfiguration config, IConfiguracionUsuarioMovilService configuracionUsuarioMovilService)
        {
            _appSettings = appSettings.Value;
            var client = new MongoClient(config.GetConnectionString("BasketJam"));
            var database = client.GetDatabase("BasketJam");
            _configuracionUsuarioMovilService = configuracionUsuarioMovilService;




            _usuarios = database.GetCollection<Usuario>("usuarios");
        }


        public async Task<Usuario> Autenticar(string username, string password)
        {
            var usuario = await _usuarios.Find<Usuario>(x => x.NomUser == username && x.Password == password).FirstOrDefaultAsync();

            // Retorno nulo si no encuentro el usuario
            if (usuario == null || usuario.EmailValidado == false)
                return null;

            // si la autenticación es correcta genero el Token JWT 
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.TopSecret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, usuario.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            usuario.Token = tokenHandler.WriteToken(token);

            return usuario;
        }

        public async Task<Usuario> Get(string id)
        {
            var _usuario = await _usuarios.Find<Usuario>(usuario => usuario.Id == id).FirstOrDefaultAsync();

            return _usuario;
        }

        public bool BuscarUsuarioPorCI(string ci)
        {
            var _usuario = _usuarios.Find<Usuario>(usuario => usuario.CI == ci).FirstOrDefaultAsync();
            // return  _usuarios.Find<Usuario>(usuario => usuario.CI == ci).FirstOrDefaultAsync();
            if (_usuario != null)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public async Task<List<Usuario>> Get()
        {
            return await _usuarios.Find(usuario => true).ToListAsync();

        }

        public async Task<Boolean> CambiarPassword(string email , string password)
        {
            try
            { 
            var UpdateDefinitionBuilder = Builders<Usuario>.Update.Set(user => user.Password, password);

           // await _usuarios.UpdateOneAsync(u => u.NomUser == email, UpdateDefinitionBuilder);

           /* Usuario user =  _usuarios.Find<Usuario>(u => u.NomUser.Equals(email)).FirstOrDefault();
                user.Password = password;
                 _usuarios.ReplaceOne(u => u.NomUser.Equals(email), user);*/

               await _usuarios.FindOneAndUpdateAsync(us => us.NomUser.Equals(email), UpdateDefinitionBuilder);

                return true;
            }
            catch(Exception ex)
            {
                return false;
            }

        }


        public async Task<Object> Create(Usuario usuario)
        {
            try
            {
                string host = "54.208.166.6";
                string scheme = "http";
                string port = "";
                /*string host = "localhost";
                string scheme = "http";
                string port = "5001";*/

                /*Inicio creación de índices*/
                IndexKeysDefinition<Usuario> keysNomUser =
               Builders<Usuario>.IndexKeys.Ascending("NombreUser");
                var optionsNomUser = new CreateIndexOptions { Name = "IndexUniqueNombreUser", Unique = true };
                var indexModelNomUser = new CreateIndexModel<Usuario>(keysNomUser, optionsNomUser);
                await _usuarios.Indexes.CreateOneAsync(indexModelNomUser);

                IndexKeysDefinition<Usuario> keysCi =
                   Builders<Usuario>.IndexKeys.Ascending("CI");
                var optionsCi = new CreateIndexOptions { Name = "IndexUniqueCI", Unique = true };
                var indexModelCi = new CreateIndexModel<Usuario>(keysCi, optionsCi);
                await _usuarios.Indexes.CreateOneAsync(indexModelCi);
                /*Fin creación de índices*/

                usuario.CodigoAutenticacion = Guid.NewGuid().ToString();


                await _usuarios.InsertOneAsync(usuario);

                if (usuario.TipoUsuario == (TipoUsuario)2)
                {
                    ConfiguracionUsuarioMovil unaConf = new ConfiguracionUsuarioMovil();
                    unaConf.EquiposFavoritos = new List<String>();
                    unaConf.NotificacionEquiposFavoritos = false;
                    unaConf.NotificacionFinPartido = false;
                    unaConf.NotificacionInicioPartido = false;
                    unaConf.NotificacionTodosLosPartidos = false;
                    unaConf.Usuario = usuario.Id;
                    await _configuracionUsuarioMovilService.CrearConfiguracionUsuarioMovil(unaConf);
                }

                SendVerificationLinkEmail(usuario.NomUser, usuario.CodigoAutenticacion.ToString(), scheme, host, port);
                string mensaje="El registro se ha realizado correctamente ,se ha enviado un link de activación a tu mail: " + usuario.NomUser;
                return new { result = true, mensaje = mensaje };

                //return usuario;
            }
            catch (MongoWriteException ex)
            {
                if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey && ex.Message.Contains("IndexUniqueCI"))
                    return (new {result=false,mensaje= "Ya existe un usuario con la C.I ingresada." });
                else if (ex.WriteError.Category == ServerErrorCategory.DuplicateKey && ex.Message.Contains("IndexUniqueNombreUser"))
                    //throw new AppException("Ya existe un usuario con el nombre de usuario ingresado.");
                    return (new { result=false,mensaje="Ya existe un usuario con el nombre de usuario ingresado." });
                else
                    throw ex;

            }
        }

        private void SendVerificationLinkEmail(string emailId, string codigoActivacion, string scheme, string host, string port)
        {
            //var varifyUrl = scheme + "://" + host + ":" + port + "/usuario/ActivateAccount/" + codigoActivacion;//esto es para pruebas locales
            var varifyUrl = scheme + "://" + host + "/usuario/ActivateAccount/" + codigoActivacion;
            var fromMail = new MailAddress("basketjam2019@gmail.com", "Basket Jam Team");
            var toMail = new MailAddress(emailId);
            var frontEmailPassowrd = "BasketJam2019";
            string subject = "¡Tu cuenta en BasketJam ha sido creada exitosamente!";
            string body = "<br/><br/>Estamos encantados de informarte que tu cuenta " +
        " fué creada exitosamente. Por favor haz click en el link debajo para verificar tu cuenta" +
        " <br/><br/><a href='" + varifyUrl + "'>" + varifyUrl + "</a> ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromMail.Address, frontEmailPassowrd)

            };
            using (var message = new MailMessage(fromMail, toMail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }


        public void SendPassReset(string emailId)
        {
            string host = "54.208.166.6";
            string scheme = "http";
            string port = "";
            //var varifyUrl = scheme + "://" + host + ":" + port + "/usuario/ActivateAccount/" + codigoActivacion;//esto es para pruebas locales
           /* var usuario =  _usuarios.Find<Usuario>(u => u.NomUser == emailId).FirstOrDefaultAsync();
            usuario..Password = null;
            _usuarios.ReplaceOne(user => user.NomUser == emailId, usuario.Result);
            _usuarios.FindOneAndReplace<Usuario>(us => us.NomUser = emailId, usuario);*/

            var UpdateDefinitionBuilder = Builders<Usuario>.Update.Set(use => use.Password, null);

             _usuarios.UpdateOneAsync(u => u.NomUser == emailId, UpdateDefinitionBuilder);

            // var varifyUrl = scheme + "://" + host + "/usuario/resetearContraseña/?" + emailId;
            var varifyUrl= "http://basketjam.s3.us-east-2.amazonaws.com/Bjam/restarurarContrase%C3%B1a.html"+"?mail="+emailId;
            var fromMail = new MailAddress("basketjam2019@gmail.com", "Basket Jam Team");
            var toMail = new MailAddress(emailId);
            var frontEmailPassowrd = "BasketJam2019";
            string subject = "¡Se ha reseteado tu contraseña!";
            string body = "<br/><br/>Para ingresar una nueva contraseña da clic en el link debajo. " +
        " <br/><br/><a href='" + varifyUrl + "'>" + varifyUrl + "</a> ";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromMail.Address, frontEmailPassowrd)

            };
            using (var message = new MailMessage(fromMail, toMail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }


        public async Task<Object> VerificarCuenta(string activationCode)
        {

            try
            {
                string str = "";
               
                /*objEntity.Configuration.ValidateOnSaveEnabled = false;            
                var value = objEntity.RegDetails.Where(a => a.ActivateionCode == new Guid(activationCode)).FirstOrDefault();*/
                var usuario = await _usuarios.Find<Usuario>(x => x.CodigoAutenticacion == activationCode).FirstOrDefaultAsync();
                if (usuario != null)
                {
                      await _usuarios.UpdateOneAsync(
                                       us => us.CodigoAutenticacion.Equals(activationCode),
                                       Builders<Usuario>.Update.
                                       Set(b => b.EmailValidado, true));


                      str = "Estimado usuario , su e-mail ha sido activado correctamente , ahora puede acceder a BasketJam con su cuenta";
                      return new { result = true, mensaje = str };
                   // return usuario;
                }
                else
                {
                    str = "Estimado usuario , su e-mail no ha podido ser activado.";
                    return new { result = false, mensaje = str };
                }

                //  return str;
            }
            catch
            {
                return new { result = false, mensaje = "Se ha producido un error inesperado." };
            }

        }
    }
}