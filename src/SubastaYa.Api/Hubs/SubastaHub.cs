using Microsoft.AspNetCore.SignalR;

namespace SubastaYa.Api.Hubs;

// Canal en vivo de las subastas. Cada subasta es un grupo: quien mira una subasta solo
// recibe los avisos de esa, no los de todas. Sin [Authorize]: la sala se mira sin estar
// logueado, igual que el detalle. El que exige token es el endpoint de oferta.
public class SubastaHub : Hub
{
    public Task UnirseASubasta(int subastaId)
        => Groups.AddToGroupAsync(Context.ConnectionId, NombreDeGrupo(subastaId));

    public Task SalirDeSubasta(int subastaId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, NombreDeGrupo(subastaId));

    // Publico y estatico: quien emite desde fuera del hub (el servicio de pujas y el
    // worker) tiene que armar exactamente el mismo nombre de grupo.
    public static string NombreDeGrupo(int subastaId) => $"subasta-{subastaId}";
}
