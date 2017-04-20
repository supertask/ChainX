using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Colopl {

	// Use this for initialization
	public void Run () {
        Character a = new Enemy();
        Character b = new Player(); 
        Player    c = new Player();

        a.Description();
        ((Enemy)a).Description();
        b.Talk();
        ((Player)b).Talk();
        c.Description();
        ((Character)c).Description();
		
	}
	
    private class Character {
            public virtual void Description() {
                    var str = "Character Class";
                    Debug.Log(str);
            }

            public void Talk() {
                    var str = "Hello";
                    Debug.Log(str);
            }
    }

    private class Enemy : Character {
            public override void Description() {
                    var str = "Enemy Class";
                    Debug.Log(str);
            }

            public void Talk() {
                    var str = "Grrr!";
                    Debug.Log(str);
            }
    }

    private class Player : Character
    {
            public override void Description() {
                    var str = "Player Class";
                    Debug.Log(str);
            }

            public void Talk() {
                    var str = "Have Fun!";
                    Debug.Log(str);
            }
    }
}
