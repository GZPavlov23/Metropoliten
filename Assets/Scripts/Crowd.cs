using UnityEngine;

// Everyone on the line tonight. The shift calls out cues and they go about their business; whoever's job it
// is to shut the doors has to wait until the last of them is finished.
public class Crowd : MonoBehaviour
{
    public Passenger[] people;

    [Tooltip("What can't be done while people are still getting on and off.")]
    public TalkSpot[] waitFor = new TalkSpot[0];

    public void Cue(string cue)
    {
        foreach (Passenger person in people) person.Cue(cue);
    }

    public void Stare(bool staring)
    {
        foreach (Passenger person in people)
            if (person.gameObject.activeInHierarchy) person.Stare(staring);
    }

    void Update()
    {
        bool busy = false;
        foreach (Passenger person in people) busy |= person.Busy;
        foreach (TalkSpot spot in waitFor) spot.waiting = busy;
    }
}
