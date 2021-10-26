namespace Amilious.Saving {
    
    public enum MissingStateType {
        //the SaveableEntity data was missing.
        SaveableEntity, 
        //the SaveableEntity data was not missing, but the ISaveable's data was missing.
        SaveableComponent,
        //the Scene data was missing
        SceneData
    }
    
}