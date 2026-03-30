public interface ISavableItem
{
    byte[] SaveState();
    void LoadState(byte[] state);
}