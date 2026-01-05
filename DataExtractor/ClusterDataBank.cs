using System.Collections.Generic;
namespace Nomad.Api.Data;

public static class ClusterDataBank
{
    public static readonly List<ClusterDefinition> Clusters = new List<ClusterDefinition>
    {
        new ClusterDefinition
        {
            Name = "Achieving Excellence",
            Description = "Achieving Excellence Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Business Acumen",
                    Description = "Business Acumen Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I stay current with the latest trends and advances in the industry.",
                            OthersQuestion = "{subjectName} stays current with the latest trends and advances in the industry."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I have a clear understanding of the factors that impact our success as a business.",
                            OthersQuestion = "{subjectName} shows a clear understanding of the factors that impact our success as a business."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I can identify connections between different functions and their role in the organization",
                            OthersQuestion = "{subjectName} demonstrates a clear understanding of the factors that impact our success as a business."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am able to identify connections between different  functions and their role in the organization.",
                            OthersQuestion = "{subjectName} is able to identify connections between different functions and their role in the organization."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Intellectual Acumen",
                    Description = "Intellectual Acumen Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I can make necessary decisions even when there is limited information.",
                            OthersQuestion = "{subjectName} can make necessary decisions even when there is limited information."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I back up my decisions with relevant information and data.",
                            OthersQuestion = "{subjectName} backs up {subjectName}’s decisions with relevant information and data."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am always in search of opportunities that can help me and my organization grow.",
                            OthersQuestion = "{subjectName} is always in search of opportunities that can help {subjectName} and the organization grow."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Problem Solving and Decision Making",
                    Description = "Problem Solving and Decision Making Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I do not give up easily even when things get difficult.",
                            OthersQuestion = "{subjectName} does not give up easily even when things get difficult."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I weigh up pros and cons of each option before making a decision.",
                            OthersQuestion = "{subjectName} weighs up pros and cons of each option before making a decision."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I provide support and stay involved along the way to get the job done.",
                            OthersQuestion = "{subjectName} provides support and stays involved along the way to get the job done."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "identifies cause of the problem to solve it timely and appropriately.",
                            OthersQuestion = "{subjectName} is able to identify the cause of the problem to solve it timely and appropriately."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I know when to take initiative and when to ask for support.",
                            OthersQuestion = "{subjectName} is able to decide when to take initiative and when to ask for support."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Entrepreneurial Innovation",
                    Description = "Entrepreneurial Innovation Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am constantly looking for new ways of doing things.",
                            OthersQuestion = "{subjectName} is constantly looking for new ways of doing things."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am able to implement ideas with planning, organizing and action.",
                            OthersQuestion = "{subjectName} implements ideas with planning, organizing, and action."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I accurately anticipate and understand future consequences to current decision.",
                            OthersQuestion = "{subjectName} anticipates and understands future consequences of current decisions."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I encourage new ideas and approaches towards efficiency and growth.",
                            OthersQuestion = "{subjectName} encourages new ideas and approaches towards efficiency and growth."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Collaborate for Success",
            Description = "Collaborate for Success Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Interpersonal Savvy",
                    Description = "Interpersonal Savvy Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I consider how my emotions can affect others.",
                            OthersQuestion = "{subjectName} considers how {subjectName}’s emotions can affect others."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I make conscious effort to build and maintain relationships with people.",
                            OthersQuestion = "{subjectName} makes an effort to build and maintain relationships with people."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I encourage team members to share information and ideas openly with one another.",
                            OthersQuestion = "{subjectName} encourages team members to share information and ideas openly with one another."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Stakeholder management",
                    Description = "Stakeholder management Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I'm able to persuade my stakeholders based on mutual needs and understanding.",
                            OthersQuestion = "{subjectName} persuades {subjectName}’s stakeholders based on mutual needs and understanding."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I leverage my networks to achieve personal and/or organizational goals.",
                            OthersQuestion = "{subjectName} leverages {subjectName}’s networks to achieve personal and/or organizational goals."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Developing Others",
                    Description = "Developing Others Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I encourage exchange of information in my team.",
                            OthersQuestion = "{subjectName} encourages exchange of information in {subjectName}’s team."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I provide clarity and support to others in achieving their goals.",
                            OthersQuestion = "{subjectName} provides clarity and support to others in achieving their goals."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I give feedback to my team members in a constructive and timely manner.",
                            OthersQuestion = "{subjectName} gives feedback to team members in a constructive and timely manner."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I'm aware of my team members' development needs and I prioritize their capability development.",
                            OthersQuestion = "{subjectName} is aware of {subjectName}’s team members’ development needs and prioritizes their capability development."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Teamwork and Collaboration",
                    Description = "Teamwork and Collaboration Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I encourage those working in different areas to pull together to achieve common goals.",
                            OthersQuestion = "{subjectName} encourages those working in different areas to pull together to achieve common goals."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am good at putting together teams of people with complementary skills.",
                            OthersQuestion = "{subjectName} is good at putting together teams of people with complementary skills."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I appreciate efforts from my team members towards achievement of shared goals and objectives.",
                            OthersQuestion = "{subjectName} appreciates efforts from team members towards achievement of shared goals and objectives."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I help my team find solutions out of conflictual situations.",
                            OthersQuestion = "{subjectName} helps the team find solutions out of conflictual situations."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Drive for Results",
            Description = "Drive for Results Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Customer Focus",
                    Description = "Customer Focus Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I seek customer and stakeholder feedback to ensure their satisfaction.",
                            OthersQuestion = "{subjectName} seeks customer and stakeholder feedback to ensure their satisfaction."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I prioritize meeting customer needs by working towards them and mobilizing resources timely.",
                            OthersQuestion = "{subjectName} prioritizes meeting customer needs by working towards them and mobilizing resources timely."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Achievement Focus",
                    Description = "Achievement Focus Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I inspire others to perform at their best and beyond what is required by setting an example myself.",
                            OthersQuestion = "{subjectName} inspires others to perform at their best and beyond what is required by setting an example."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I set stretch goals for my team that are linked with organizational goals.",
                            OthersQuestion = "{subjectName} sets stretch goals for the team that are linked with organizational goals."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Commitment to Continuous Improvement",
                    Description = "Commitment to Continuous Improvement Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am constantly looking for ways to change the way things are done in the organization.",
                            OthersQuestion = "{subjectName} is constantly looking for ways to change the way things are done in the organization."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am able to identify potential performance problems early",
                            OthersQuestion = "{subjectName} identifies potential performance problems early."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I look at devised improvement plans holistically to understand its feasibility and gauge its impact.",
                            OthersQuestion = "{subjectName} looks at devised improvement plans holistically to understand feasibility and gauge impact."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Personal Mastery",
            Description = "Personal Mastery Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Integrity",
                    Description = "Integrity Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I can be counted on to follow through promises.",
                            OthersQuestion = "{subjectName} can be counted on to follow through with {subjectName}’s promises."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I maintain confidentiality and credibility in stressful situations.",
                            OthersQuestion = "{subjectName} maintains confidentiality and credibility in stressful situations."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I set a good example of the behavior asked for",
                            OthersQuestion = "{subjectName} sets a good example of the behavior {subjectName} asks for."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I consistently apply our organization's policies to avoid double standards.",
                            OthersQuestion = "{subjectName} consistently applies our organization’s policies to avoid double standards."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I do not blame others or situations for my mistakes.",
                            OthersQuestion = "{subjectName} does not blame others or situations for {subjectName}’s mistakes."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Self-Awareness",
                    Description = "Self-Awareness Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I seek corrective feedback to improve myself.",
                            OthersQuestion = "{subjectName} seeks corrective feedback to improve."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I sort out my strengths and weaknesses fairly accurately",
                            OthersQuestion = "{subjectName} sorts out {subjectName}’s strengths and weaknesses fairly accurately (i.e., knows {subjectName}’s self)."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am aware of the impact of myself on other people",
                            OthersQuestion = "{subjectName} is aware of the impact of self on other people."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I contribute more to solving organizational problems than to complaining about them.",
                            OthersQuestion = "{subjectName} contributes more to solving organizational problems than to complaining about them."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Resilience",
                    Description = "Resilience Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I do not become hostile or moody when things are not going my way",
                            OthersQuestion = "{subjectName} does not become hostile or moody when things are not going {subjectName}’s way."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I adjust plan according to monitoring results & changing priorities.",
                            OthersQuestion = "{subjectName} adjusts plans according to monitoring results and changing priorities."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am prepared to seize opportunities when they arise.",
                            OthersQuestion = "{subjectName} is prepared to seize opportunities when they arise."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Passion & Energy",
                    Description = "Passion & Energy Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I have the passion to make a difference in the organization.",
                            OthersQuestion = "{subjectName} has the passion to make a difference in the organization."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am assertive and energetic.",
                            OthersQuestion = "{subjectName} is assertive and energetic."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I help create a positive working environment that encourages people to work to their full potential.",
                            OthersQuestion = "{subjectName} helps create a positive working environment that encourages people to work to their full potential."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Leading People",
            Description = "Leading People Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Employee Engagement",
                    Description = "Employee Engagement Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I listen to employees both when things are going well and when they are not.",
                            OthersQuestion = "{subjectName} listens to employees both when things are going well and when they are not."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am comfortable managing people from different racial or cultural backgrounds.",
                            OthersQuestion = "{subjectName} is comfortable managing people from different racial or cultural backgrounds."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I push decision making to the lowest appropriate level and develops employees' confidence in their abilities.",
                            OthersQuestion = "{subjectName} pushes decision-making to the lowest appropriate level and develops employees’ confidence in their abilities."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I provide prompt feedback, both positive and negative.",
                            OthersQuestion = "{subjectName} provides prompt feedback, both positive and negative."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Managing Performance &  Accountability",
                    Description = "Managing Performance &  Accountability Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I correctly identifies potential performance problems early.",
                            OthersQuestion = "{subjectName} correctly identifies potential performance problems early."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I effectively uses goals and performance indicators to drive improved performance.",
                            OthersQuestion = "{subjectName} effectively uses goals and performance indicators to drive improved performance."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I model a high-performance work ethic & constant self-improvement.",
                            OthersQuestion = "{subjectName} models a high-performance work ethic and constant self-improvement."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I hold people accountable to the organization's values and expectations.",
                            OthersQuestion = "{subjectName} holds people accountable to the organization’s values and expectations."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I emphasize performance and delivery of outcomes.",
                            OthersQuestion = "{subjectName} emphasizes performance and delivery of outcomes."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am open to the input of others.",
                            OthersQuestion = "{subjectName} is open to the input of others."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Teambuilding &  Collaboration",
                    Description = "Teambuilding &  Collaboration Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I use effective listening skills to gain clarification from others.",
                            OthersQuestion = "{subjectName} uses effective listening skills to gain clarification from others."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I get things done without creating unnecessary adversarial relationships",
                            OthersQuestion = "{subjectName} gets things done without creating unnecessary adversarial relationships."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I use my knowledge base to broaden the range of problem-solving options for direct reports.",
                            OthersQuestion = "{subjectName} uses {subjectName}’s knowledge base to broaden the range of problem-solving options for direct reports."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I involve others in the beginning stages of an initiative.",
                            OthersQuestion = "{subjectName} involves others in the beginning stages of an initiative."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I build trust and loyalty with others",
                            OthersQuestion = "{subjectName} builds trust and loyalty with others."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I encourage those working in different areas to pull together to achieve common goals",
                            OthersQuestion = "{subjectName} encourages those working in different areas to pull together to achieve common goals."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am good at reading the audience and adapting their communication style accordingly.",
                            OthersQuestion = "{subjectName} is good at reading the audience and adapting communication style accordingly."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Open Communication",
                    Description = "Open Communication Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I choose the appropriate method of communication for the situation.",
                            OthersQuestion = "{subjectName} chooses the appropriate method of communication for the situation."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I listen actively without interrupting.",
                            OthersQuestion = "{subjectName} listens actively without interrupting."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I know when and how to express emotion.",
                            OthersQuestion = "{subjectName} knows when and how to express emotion."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I make necessary decisions even when there is limited information.",
                            OthersQuestion = "{subjectName} makes necessary decisions even when there is limited information."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Delivering Results",
            Description = "Delivering Results Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Decision Making",
                    Description = "Decision Making Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I weigh up pros and cons of each option before making a decision.",
                            OthersQuestion = "{subjectName} weighs up pros and cons of each option before making a decision."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I make appropriate decisions swiftly.",
                            OthersQuestion = "{subjectName} makes appropriate decisions swiftly."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I analyze complex situation carefully, then reduces it to its simplest terms in searching for a solution",
                            OthersQuestion = "{subjectName} analyzes complex situations carefully, then reduces them to simplest terms when searching for a solution."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I know when discussions need to turn to action",
                            OthersQuestion = "{subjectName} knows when discussions need to turn to action."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I do not feel overwhelmed when facing action.",
                            OthersQuestion = "{subjectName} does not feel overwhelmed when facing action."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I perform to a high standard on challenging assignments.",
                            OthersQuestion = "{subjectName} performs to a high standard on challenging assignments."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Result-Focused",
                    Description = "Result-Focused Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I consistently delivers good results.",
                            OthersQuestion = "{subjectName} consistently delivers good results."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I check work to ensure accuracy and completeness.",
                            OthersQuestion = "{subjectName} checks work to ensure accuracy and completeness."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I manage to work around processes to deliver required results on time.",
                            OthersQuestion = "{subjectName} manages to work around processes to deliver required results on time."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I look for opportunities to help move a project along.",
                            OthersQuestion = "{subjectName} looks for opportunities to help move a project along."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Continuous Improvement",
                    Description = "Continuous Improvement Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I always looks for opportunities to deliver better results",
                            OthersQuestion = "{subjectName} always looks for opportunities to deliver better results."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I effectively builds and maintains feedback channels.",
                            OthersQuestion = "{subjectName} effectively builds and maintains feedback channels."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I try different and new ways to deal with problems & opportunities.",
                            OthersQuestion = "{subjectName} tries different and new ways to deal with problems and opportunities."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I challenge status-quo and looks for opportunities to improve",
                            OthersQuestion = "{subjectName} challenges the status quo and looks for opportunities to improve."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I adapt to the changing external pressures facing the organization.",
                            OthersQuestion = "{subjectName} adapts to changing external pressures facing the organization."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Creating the Future",
            Description = "Creating the Future Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Business Focus & External Awareness",
                    Description = "Business Focus & External Awareness Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I stay current with the latest trends and advances in his/her industry or field.",
                            OthersQuestion = "{subjectName} stays current with the latest trends and advances in {subjectName}’s industry or field."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I have a clear understanding of the factors that impact our success as a business.",
                            OthersQuestion = "{subjectName} demonstrates a clear understanding of the factors that impact the organization’s success as a business."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am respected as a talented and knowledgeable person in my area of responsibility.",
                            OthersQuestion = "{subjectName} is respected as a talented and knowledgeable person in {subjectName}’s area of responsibility."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am able to link my responsibilities with the results of the organization",
                            OthersQuestion = "{subjectName} is able to link {subjectName}’s responsibilities with organizational results."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Strategic Thinking",
                    Description = "Strategic Thinking Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am effective in setting long-term stretch goals linked with organizational vision.",
                            OthersQuestion = "{subjectName} is very effective in setting long-term stretch goals linked with the organization’s vision."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "Once the more obvious problems in an assignment are solved, I can see the underlying problems",
                            OthersQuestion = "Once the more obvious problems in an assignment are solved, {subjectName} can see the underlying problems."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I adjust management style to changing situations.",
                            OthersQuestion = "{subjectName} adjusts management style to changing situations."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Leading Change",
                    Description = "Leading Change Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am able to recognise when a chosen course of action is no longer possible and will reassess the alternatives",
                            OthersQuestion = "{subjectName} is able to recognize when a chosen course of action is no longer possible and reassesses alternatives."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I own, promote and communicate vision of the organization.",
                            OthersQuestion = "{subjectName} owns, promotes, and communicates the organization’s vision."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I lead change by example.",
                            OthersQuestion = "{subjectName} leads change by example."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I feel proud when I tell others that I am part of this organization",
                            OthersQuestion = "{subjectName} feels proud when telling others that {subjectName} is part of the organization."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Engagement of Self",
                    Description = "Engagement of Self Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I would recommend my organization to friends and relatives as a great place to work.",
                            OthersQuestion = "Would you recommend the organization to friends and relatives as a great place to work?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am currently not thinking of leaving this organization for a better opportunity.",
                            OthersQuestion = "Are you currently thinking of leaving the organization for a better opportunity?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "My organization motivates me to put in extra effort (without any rewards) to achieve its objectives",
                            OthersQuestion = "Does the organization motivate you to put in extra effort (without any rewards) to achieve its objectives?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "If I were to leave, I would miss my friends and people I work with more than anything else.",
                            OthersQuestion = "If you were to leave, would you miss your friends and the people you work with more than anything else?"
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Dynamic Sensing",
            Description = "Dynamic Sensing Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Inventive Execution",
                    Description = "Inventive Execution Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I act with integrity and ownership",
                            OthersQuestion = "{subjectName} acts with integrity and sets ethical standards across the organization."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am focused on quality results",
                            OthersQuestion = "{subjectName} enables teams to deliver high-quality outcomes by aligning cross-functional goals, overcoming organizational barriers, and setting standards that promote a culture of continuous improvement and safety."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I manage stress effectively",
                            OthersQuestion = "{subjectName} maintains composure, empathy, and confidence during crises and pressure situations."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Expansive Thinking",
                    Description = "Expansive Thinking Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I analyze to solve",
                            OthersQuestion = "{subjectName} enables and inspires the team to face complex challenges with critical thinking and collaboration."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I manage resources effectively",
                            OthersQuestion = "{subjectName} optimizes resources across teams for strategic impact and efficiency."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I plan proactively",
                            OthersQuestion = "{subjectName} embeds long-term planning in organizational practices and guides teams to navigate disruption."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Decision Making",
                    Description = "Decision Making Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I make informed decisions",
                            OthersQuestion = "{subjectName} involves key stakeholders in strategic decisions to ensure alignment and ownership."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I manage uncertainty",
                            OthersQuestion = "{subjectName} drives the organization forward by making timely, strategic decisions even under uncertainty."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am firm in my decisions",
                            OthersQuestion = "{subjectName} takes decisive action with clarity and communicates decisions effectively."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Dynamic Sensing",
                    Description = "Dynamic Sensing Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I maintain composure in crises.",
                            OthersQuestion = "{subjectName} does not become overwhelmed when making decisions during a crisis."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I respond effectively to shifting customer needs and expectations",
                            OthersQuestion = "{subjectName} responds effectively to shifting customer needs and expectations."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am able to manage with changing scenarios",
                            OthersQuestion = "{subjectName} continually reviews problems and solutions in light of changing situations, and shifts course as needed."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I adjust management style in response to changing internal and external factors.",
                            OthersQuestion = "{subjectName} adjusts management style in response to changing internal and external factors."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "People Agility",
            Description = "People Agility Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Nurture Growth & Teamwork",
                    Description = "Nurture Growth & Teamwork Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I collaborate for team goals",
                            OthersQuestion = "{subjectName} fosters a culture of cross-organizational collaboration to achieve strategic goals."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I recognize efforts",
                            OthersQuestion = "{subjectName} actively recognizes contributions and encourages learning from both success and failure."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I act with empathy",
                            OthersQuestion = "{subjectName} promotes empathy and inclusion, especially during periods of transformation."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Effective Conversations",
                    Description = "Effective Conversations Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I communicate with clarity",
                            OthersQuestion = "{subjectName} communicates organizational strategy and vision with clarity and conviction."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I value feedback",
                            OthersQuestion = "{subjectName} establishes constructive feedback as a norm at all levels."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I express with courage and authenticity",
                            OthersQuestion = "{subjectName} leads courageous conversations that build trust and authenticity."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Network & Influence",
                    Description = "Network & Influence Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I negotiate with influence",
                            OthersQuestion = "{subjectName} leads high-stakes negotiations and effectively influences key stakeholders."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I network for opportunities",
                            OthersQuestion = "{subjectName} builds strategic networks across and beyond the organization to enable growth."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I liaise with confidence",
                            OthersQuestion = "{subjectName} inspires trust and confidence across diverse audiences with executive presence."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "People Agility",
                    Description = "People Agility Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I use effective listening.",
                            OthersQuestion = "{subjectName} uses effective listening skills to gain clarification from others."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I do not micro manage.",
                            OthersQuestion = "{subjectName} uses persuasion and influence instead of micromanaging employees."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I respond effectively to criticism",
                            OthersQuestion = "{subjectName} responds effectively to constructive criticism from others."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I get things done without creating adversarial relationships.",
                            OthersQuestion = "{subjectName} gets things done without creating unnecessary adversarial relationships."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Organizational Relatability",
            Description = "Organizational Relatability Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Continuous Excellence",
                    Description = "Continuous Excellence Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am driven to outperform",
                            OthersQuestion = "{subjectName} champions a culture of innovation and continuous improvement to meet strategic goals."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I adapt to change",
                            OthersQuestion = "{subjectName} leads change with agility, ensuring the organization adapts effectively to evolving needs."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am resilient and positive",
                            OthersQuestion = "{subjectName} shows resilience and determination in the face of adversity or ambiguity."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Active Learning",
                    Description = "Active Learning Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I challenge the status quo",
                            OthersQuestion = "{subjectName} challenges existing norms and drives breakthrough thinking to stay ahead of the curve."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am curious to learn",
                            OthersQuestion = "{subjectName} promotes a learning culture by sharing insights and encouraging others to apply learnings."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I apply learnings to action",
                            OthersQuestion = "{subjectName} strategically invests in leadership development to build future capabilities."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Strategic Thinking",
                    Description = "Strategic Thinking Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am customer-oriented",
                            OthersQuestion = "{subjectName} champions a customer-centric culture throughout the organization, emphasizing the strategic importance of customer satisfaction."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I seek opportunities for growth",
                            OthersQuestion = "{subjectName} pursues transformative strategies that position the organization for long-term success."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am committed to the purpose",
                            OthersQuestion = "{subjectName} embodies the organization’s mission and inspires teams to contribute to its long-term purpose."
                        },
                    }
                },
                new CompetencyDefinition
                {
                    Name = "Organizational Relatability",
                    Description = "Organizational Relatability Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I stay upto date with industry trends.",
                            OthersQuestion = "{subjectName} stays current with the latest trends and advancements in the industry."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I adapt positively to pressure.",
                            OthersQuestion = "{subjectName} adapts positively to changing internal and external pressures facing the organization."
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am good with changing situations.",
                            OthersQuestion = "{subjectName} accurately evaluates the implications of new information or events, and adapts plans as necessary."
                        },
                    }
                },
            }
        },
        new ClusterDefinition
        {
            Name = "Engagement of Self",
            Description = "Engagement of Self Cluster",
            Competencies = new List<CompetencyDefinition>
            {
                new CompetencyDefinition
                {
                    Name = "Open Ended Questions",
                    Description = "Open Ended Questions Competency",
                    Questions = new List<QuestionDefinition>
                    {
                        new QuestionDefinition
                        {
                            SelfQuestion = "I feel proud when I tell others that I am part of the organization.",
                            OthersQuestion = "Do you feel proud when you tell others that you are a part of the organization?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I would recommend my organization to friends and relatives as a great place to work",
                            OthersQuestion = "Would you recommend the organization to friends and relatives as a great place to work?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "I am currently not thinking of leaving the organization for a better opportunity",
                            OthersQuestion = "Are you currently thinking of leaving the organization for a better opportunity?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "My organization motivates me to put in extra effort(without any rewards) to achieve its objectives.",
                            OthersQuestion = "Does the organization motivate you to put in extra effort (without any rewards) to achieve its objectives?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "If I were to leave, I would miss my friends and people I work with more than anything else",
                            OthersQuestion = "If you were to leave, would you miss your friends and the people you work with more than anything else?"
                        },
                        new QuestionDefinition
                        {
                            SelfQuestion = "What should I continue doing in order to become more effective as a leader",
                            OthersQuestion = "What should {subjectName} continue doing in order to become more effective as a leader?"
                        },
                    }
                },
            }
        },
    };
}

public class ClusterDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CompetencyDefinition> Competencies { get; set; } = new();
}

public class CompetencyDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<QuestionDefinition> Questions { get; set; } = new();
}

public class QuestionDefinition
{
    public string SelfQuestion { get; set; } = string.Empty;
    public string OthersQuestion { get; set; } = string.Empty;
}
