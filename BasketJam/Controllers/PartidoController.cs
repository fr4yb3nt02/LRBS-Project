using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BasketJam.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class PartidoController : ControllerBase
    {
      private IPartidoService _partidoService;

        public PartidoController(IPartidoService partidoService)
        {
            _partidoService = partidoService;
        }

       [HttpGet]
        public async Task<ActionResult<List<Partido>>> Get()
        {
            return await _partidoService.ListarPartidos();
        }

        [HttpGet("{id:length(24)}", Name = "ObtenerPartido")]
        public async Task<ActionResult<Partido>> Get(string id)
        {
            var partido =await _partidoService.BuscarPartido(id);

            if (partido == null)
            {
                return NotFound();
            }

            return partido;
        }

        [HttpPost]
        public async Task<ActionResult<Partido>> Create(Partido partido)
        {
            
            await _partidoService.CrearPartido(partido);

            return CreatedAtRoute("ObtenerPartido", new { id = partido.Id.ToString() }, partido);
        }

        [HttpPut("{id:length(24)}")]
        public IActionResult Update(string id, Partido partidoIn)
        {
            var partido = _partidoService.BuscarPartido(id);

            if (partido == null)
            {
                return NotFound();
            }

            _partidoService.ActualizarPartido(id,partidoIn);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public IActionResult Delete(string id)
        {
            var partido = _partidoService.BuscarPartido(id);

            if (partido == null)
            {
                return NotFound();
            }

            _partidoService.EliminarPartido(partido.Id.ToString());

            return NoContent();
        }


        [HttpPut("AgregarJuezAPartido/{id:length(24)}")]
       // [HttpPut("{id:length(24)}", Name = "AgregarJuezAPartido")]
        public async Task<ActionResult<bool>> AgregarJuezAPartido(string id,[FromBody]List<Juez> jueces)
        {
            var partido = await _partidoService.BuscarPartido(id);

            if (partido == null)
            {
                return NotFound();
            }

           await _partidoService.AgregarJuezPartida(id,jueces);

            return Ok();
        }

[AllowAnonymous]
 [HttpGet("Listpart")]
                public async Task<ActionResult>  visualizadorPartidos()
        {

          //  List<String> a = await _partidoService.DevuelvoListPartidosAndroid();
            return Ok(await _partidoService.DevuelvoListPartidosAndroid());
    //return Ok(_partidoService.DevuelvoListPartidosAndroid);
}
    }
}