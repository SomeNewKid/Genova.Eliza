// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.UnitTests;

public class ElizaEngine_Tests
{
    [Fact]
    public void Outputs_are_deterministic()
    {
        ElizaEngine e1 = new ();
        ElizaEngine e2 = new ();

        string[] inputs = ["I feel sad.", "Yes", "I am angry."];

        string[] out1 = inputs.Select(i => e1.Reply(i)).ToArray();
        string[] out2 = inputs.Select(i => e2.Reply(i)).ToArray();

        Assert.Equal(out1, out2);
    }

    [Fact]
    public void Dream_template_pronoun_must_not_change()
    {
        // Repro for: “What does that dream suggest to you?” becoming “… to i?”
        ElizaEngine engine = new ();
        string input = "I had a dream.";

        string reply = engine.Reply(input);

        Assert.Contains("to you?", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("to i?", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Elaboration_template_pronoun_must_not_change()
    {
        // Repro for: “Can you elaborate on that?” becoming “Can i elaborate on that?”
        ElizaEngine engine = new();
        string input = "I am angry.";

        string reply = engine.Reply(input);

        Assert.Contains("you", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("I", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Memory_primary_pronouns_remain_user_perspective()
    {
        ElizaEngine engine = new();

        // Turn 1: enqueue memory via (0 YOUR 0) pattern.
        _ = engine.Reply("My family is annoying me.");

        // Turn 2: trigger NONE so memory emits.
        string reply = engine.Reply("Okay.");

        Assert.Contains("your family is annoying you", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("annoying i", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Comprehensive_conversation()
    {
        string[] inputs =
        [
            "Hello, I need help.",
            "I wonder what you think.",
            "I often ask what this means.",
            "Perhaps I should rest.",
            "Maybe I overreact sometimes.",
            "I will tell you my name now.",
            "I feel sad today.",
            "I feel happy sometimes.",
            "I don't know what to do.",
            "I can't sleep at night.",
            "I am like my father.",
            "I was late again.",
            "We were arguing yesterday.",
            "I think I am angry.",
            "Yes, I agree.",
            "No, I disagree.",
            "I wonder why don't I try harder.",
            "I wonder why can't you understand me.",
            "I did it because I was scared.",
            "I worry if I fail this exam.",
            "I remember the day we met.",
            "I recollect the time we traveled.",
            "I always forget my keys.",
            "I think we are alike in many ways.",
            "I feel the same as before.",
            "I use computers at work.",
            "I work with machines all day.",
            "I like my computer a lot.",
            "I dreamed about falling.",
            "I dreamt about flying.",
            "I had a dream last night.",
            "I have dreams about school.",
            "I think you're helpful.",
            "I'm worried about my job.",
            "I think you are kind.",
            "I told you my secrets.",
            "I wonder, are you truly listening.",
            "I ask, are you a human.",
            "I ask whether people are fair.",
            "I am talking about my mother.",
            "I worry about my father.",
            "I miss my sister.",
            "I argued with my brother.",
            "I think my wife is upset.",
            "I love my children.",
            "I talked to my mom yesterday.",
            "I called my dad last night.",
            "I sent a note to my mama.",
            "I heard from my papa.",
            "I feel unhappy today.",
            "I feel depressed lately.",
            "I feel sick this week.",
            "I think everyone ignores me.",
            "I think everybody knows me.",
            "I think nobody understands me.",
            "I think noone listens to me.",
            "I asked how this works.",
            "I asked when we begin.",
            "I certainly agree with you.",
            "I spoke deutsch at school.",
            "I studied francais in college.",
            "I can read italiano a little.",
            "I hear espanol at work.",
            "Hello there, I have a question.",
            "I think what we want is unclear.",
            "Perhaps I was mistaken.",
            "Maybe I can change.",
            "I changed my name last year.",
            "I feel anxious before meetings.",
            "I don't like crowds.",
            "I cannot focus today.",
            "I can’t sleep when it rains.",
            "I am like my sister in some ways.",
            "I was thinking about you.",
            "We were thinking about moving.",
            "I am calm now.",
            "Yes, I can do that.",
            "No, I won’t do that.",
            "I wonder why don't I listen better.",
            "I wonder why can't you reply faster.",
            "I left because I was tired.",
            "I would be happy if I passed.",
            "I remember my childhood.",
            "I always worry about money.",
            "I am like my boss when stressed.",
            "I see everyone as a rival.",
            "I see nobody as a threat.",
            "I dream of peace.",
            "I dream often these days.",
            "I dreamed I was swimming.",
            "I dreamt I was flying a kite.",
            "I have recurring dreams of exams.",
            "I think you're right about that.",
            "I'm hopeful about the future.",
            "I told you I care.",
            "I feel you ignore me sometimes.",
            "I think we are the same.",
            "I find us alike in outlook.",
            "I use a machine every day.",
            "I replaced the computers at home.",
            "I asked how you feel today.",
            "I asked when you will call.",
            "I believe we are connected.",
            "I wish to know more.",
            "I think you don't care.",
            "I think you can't help.",
            "I feel you are distant.",
            "I thought about the connection.",
            "I told myself to relax.",
            "I told my mother about therapy.",
            "I told my father about my job.",
            "I told my sister about school.",
            "I told my brother about the move.",
            "I told my wife about the plan.",
            "I told my children a story.",
            "I talked with my mother again.",
            "I talked with my father again.",
            "I can remember the noise.",
            "I remember that place well.",
            "I recollect feeling nervous.",
            "I think what happened was strange.",
            "I wonder what you expect.",
            "I wonder what answer would please you.",
            "I asked what I should do.",
            "I asked what comes to mind.",
            "Perhaps I should apologize.",
            "Maybe I should listen first.",
            "I know my name is unusual.",
            "I changed my name twice.",
            "I signed my name badly.",
            "I think you're being fair.",
            "I think you're not listening.",
            "I am like my mother when tired.",
            "I am like my father when angry.",
            "I am like my sister when rushed.",
            "I am like my brother when stressed.",
            "I am like my wife when busy.",
            "I am like my children when playful.",
            "I was told to wait.",
            "We were told to leave.",
            "I always arrive early.",
            "I always forget details.",
            "I always doubt myself.",
            "I always compare myself to others.",
            "I feel sick today.",
            "I feel depressed this winter.",
            "I feel sad around holidays.",
            "I feel unhappy at work.",
            "I feel anxious in crowds.",
            "I say things I regret.",
            "I don't want to argue.",
            "I don't understand you.",
            "I don't think I can stop.",
            "I can't explain why.",
            "I can't decide quickly.",
            "I cannot imagine it.",
            "I can't accept this.",
            "I can't remember names.",
            "I can't forgive easily.",
            "I can't trust anyone.",
            "I asked why don't I just quit.",
            "I asked why can't you see it.",
            "I left because it was noisy.",
            "I stayed because it felt safe.",
            "I would go if I could.",
            "I will try if you help.",
            "I dream every weekend.",
            "I dreamed about storms.",
            "I dreamt about success.",
            "I write down my dreams.",
            "I think everyone is tired.",
            "I think everybody is late.",
            "I think nobody arrived.",
            "I think noone called.",
            "I use computers for art.",
            "I repair computers for friends.",
            "I program machines at work.",
            "I operate a machine at the lab.",
            "I like how computers solve problems.",
            "I like how machines repeat tasks.",
            "I asked how this similarity matters.",
            "I asked when this began.",
            "I believe we are alike in values.",
            "I feel the same way today.",
            "Yes, I believe so.",
            "No, I don't think so.",
            "I wonder, are you serious.",
            "I wonder, are you there now.",
            "I remember when we first met.",
            "I remember the noise of the city.",
            "I remember what you said.",
            "I remember how I felt.",
            "I regret that my mother cried.",
            "I regret that my father left.",
            "I regret that my sister moved.",
            "I regret that my brother yelled.",
            "I regret that my wife worried.",
            "I regret that my children fought.",
            "I speak francais with friends.",
            "I practice espanol every day.",
            "I practiced deutsch last week.",
            "I studied italiano years ago.",
            "I think you're being honest.",
            "I think you're mistaken.",
            "I'm feeling uncertain today.",
            "I'm feeling optimistic now.",
            "I'm thinking about my future.",
            "I'm reflecting on my habits.",
            "I'm considering a career change.",
            "I'm feeling pressure at work.",
            "I'm remembering our last talk.",
            "I'm recalling a strange dream.",
            "I always get nervous before calls.",
            "I always leave things unfinished.",
            "I always overthink decisions.",
            "I always misplace my wallet.",
            "I always compare myself to peers.",
            "I like how my brother encourages me.",
            "I like how my mother listens.",
            "I like how my father advises me.",
            "I like how my sister challenges me.",
            "I like how my wife supports me.",
            "I like how my children inspire me.",
            "I feel everyone expects too much.",
            "I feel nobody notices my effort.",
            "I feel everybody judges me.",
            "I feel noone trusts me.",
            "I wonder why don't I relax more.",
            "I wonder why can't you accept that.",
            "I stayed because it seemed right.",
            "I left because I felt lost.",
            "I will help if I can.",
            "I will change if it helps.",
            "I used a computer all morning.",
            "I watched a machine assemble parts.",
            "I remembered the smell of rain.",
            "I recollected a childhood song.",
            "Hello, I think we should start.",
            "Perhaps I misheard you earlier.",
            "Maybe I misunderstood the point.",
            "I updated my name on the form.",
            "I asked what else comes to mind.",
            "I asked how you reached that view.",
            "I asked when you first noticed it.",
            "I believe we were alike as kids.",
            "I dreamt I was late to class.",
            "I dreamed I was on stage.",
            "I keep a journal of dreams.",
            "I feel we are the same sometimes.",
        ];

        ElizaEngine engine = new();

        foreach(string input in inputs)
        {
            string reply = engine.Reply(input);

            ElizaInvariants.AssertNoLowercaseI(reply);
            ElizaInvariants.AssertStartsWithCapital(reply);
            ElizaInvariants.AssertPerspectiveFlip(input, reply);
        }
    }
}
