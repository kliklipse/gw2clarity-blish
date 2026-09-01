namespace GW2ClarityBlish.Services;

// Adapter concret vers le module Blish HUD "ArcDPS Bridge".
// TODO: cabler sur l'API reelle du module ArcDPS Bridge installe (Tache 11) —
// a verifier contre le SDK Blish HUD courant, l'API a pu evoluer depuis l'ecriture de ce plan.
public class ArcdpsBridgeBuffEventSource : IBuffEventSource
{
    public event Action<uint, int>? BuffStackChanged;
}
