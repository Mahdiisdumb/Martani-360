using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
//maybe if i puilled those namespaces in it would work?
//didnt fucking work🫩🥀

public class quithandler : MonoBehaviour//forgot this was fucking british
{
    public void act(/*Rather fucking ExitGame()* but changed so it works here*/)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    } //pulled from an old project why the fuck is it like ts🥀
    //are we fucking serious it works NOW!
    public void buttonhandler()
    {
        act();
    }
    //is this how it fucking works? for some reason with the onclick thing on the button it just wont work
//why is the ai being like a fucking idiot🫩🥀

}