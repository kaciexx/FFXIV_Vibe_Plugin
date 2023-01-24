# TODO & IDEAS

## IDEA 1
Make triggers not trigger and overwrite each others if they are of the same priority? But also make triggers be able to overwrite each other if one trigger has a higher priority over the other?

For example:
With a HPChange trigger that triggers when HP Min is at 10% and HP Max is at 30%. Any time HP changes between 10% and 30% (ex. enemy hitting, or poison tick), the trigger will retrigger, causing the vibration pattern to restart. It'd be nice to allow the pattern to finish before sending another trigger signal 🙂

## IDEA 1 - Kaciexx solution
if newTrigger.Priority == currentTrigger.Priority and 
   currentTrigger.CantBeOverwritten and
   currentTrigger.HasNotEnded
        ignore newTrigger

## To be discussed
- Feature to export trigger (without TriggerDevice). Export feature ?
