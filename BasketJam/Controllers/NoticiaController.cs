using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BasketJam.Services;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("[controller]")]
    public class NoticiaController : ControllerBase
    {
      private INoticiaService _noticiaService;

        public NoticiaController(INoticiaService noticiaService)
        {
            _noticiaService = noticiaService;
        }

       [HttpGet]
        public async Task<ActionResult<List<Noticia>>> Get()
        {
            return await _noticiaService.ListarNoticias();
        }

        [HttpGet]
        public async Task<ActionResult<List<Noticia>>> Get(DateTime fecha)
        {
            return await _noticiaService.ListarNoticiasPorFecha(fecha);
        }


        [HttpPost("crearNoticia")]
        public async Task<ActionResult<Noticia>> Create(Noticia noticia)
        {
            await _noticiaService.CrearNoticia(noticia);

            return noticia;
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Noticia noticiaIn)
        {
            var noticia = _noticiaService.BuscarNoticia(id);

            if (noticia == null)
            {
                return NotFound();
            }

            _noticiaService.ActualizarNoticia(id,noticiaIn);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public IActionResult Delete(string id)
        {
            var noticia = _noticiaService.BuscarNoticia(id);

            if (noticia == null)
            {
                return NotFound();
            }

            _noticiaService.EliminarNoticia(noticia.Id.ToString());

            return NoContent();
        }
    }
}