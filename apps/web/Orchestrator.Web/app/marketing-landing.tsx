"use client";

import { MarketingHero } from "@/components/marketing-hero";
import { PersonaCategoryCard } from "@/components/persona-category-card";
import { ChatMockup } from "@/components/chat-mockup";
import { HowItWorks } from "@/components/how-it-works";
import { Button } from "@/components/ui/button";
import { 
  Briefcase, 
  Palette, 
  Code, 
  Heart,
  ArrowRight,
  MessageSquare,
} from "lucide-react";

interface Category {
  id: string;
  name: string;
}

interface PersonaFeedItem {
  id: string;
  displayName: string;
  firstName?: string;
  lastName?: string;
  profileImageUrl?: string;
  topicCount: number;
  chatCount: number;
  messageCount: number;
  categories: Category[];
  createdAt: string;
}

interface MarketingLandingProps {
  personas: PersonaFeedItem[];
  categories: Category[];
  totalPersonaCount: number;
  totalChatCount: number;
}

export function MarketingLanding({
  personas,
  categories,
  totalPersonaCount,
  totalChatCount,
}: MarketingLandingProps) {
  // Organize personas by category groups
  const categoryGroups = [
    {
      title: "Expert Advisors",
      description: "Get professional guidance from business, finance, and legal experts",
      icon: Briefcase,
      categoryNames: ["Business", "Finance", "Legal", "Marketing", "Sales"],
      gradient: "bg-gradient-to-br from-blue-50/50 to-indigo-50/50 dark:from-blue-950/20 dark:to-indigo-950/20",
    },
    {
      title: "Creative Minds",
      description: "Collaborate with artists, writers, and designers for creative inspiration",
      icon: Palette,
      categoryNames: ["Art", "Writing", "Design", "Music", "Photography"],
      gradient: "bg-gradient-to-br from-purple-50/50 to-pink-50/50 dark:from-purple-950/20 dark:to-pink-950/20",
    },
    {
      title: "Tech Gurus",
      description: "Learn from technology experts and programming specialists",
      icon: Code,
      categoryNames: ["Technology", "Programming", "Science", "Engineering", "Data Science"],
      gradient: "bg-gradient-to-br from-green-50/50 to-emerald-50/50 dark:from-green-950/20 dark:to-emerald-950/20",
    },
    {
      title: "Wellness Guides",
      description: "Connect with health, fitness, and lifestyle professionals",
      icon: Heart,
      categoryNames: ["Health", "Fitness", "Lifestyle", "Nutrition", "Mental Health"],
      gradient: "bg-gradient-to-br from-rose-50/50 to-orange-50/50 dark:from-rose-950/20 dark:to-orange-950/20",
    },
  ];

  // Map categories to get personas for each group
  const categoryGroupsWithPersonas = categoryGroups.map((group) => {
    const matchingCategoryIds = categories
      .filter((cat) => group.categoryNames.includes(cat.name))
      .map((cat) => cat.id);

    const groupPersonas = personas.filter((persona) =>
      persona.categories.some((cat) => matchingCategoryIds.includes(cat.id))
    );

    return {
      ...group,
      personas: groupPersonas,
      count: groupPersonas.length,
    };
  });

  // Sample chat conversations
  const chatExamples = [
    {
      personaName: "Business Strategy Expert",
      title: "Strategic Planning",
      messages: [
        {
          role: "user" as const,
          content: "How should I approach market expansion for my startup?",
        },
        {
          role: "assistant" as const,
          content: "Start with thorough market research. Identify your target demographics, analyze competitors, and validate demand through surveys. Then create a phased expansion plan with clear milestones and metrics to track success.",
        },
        {
          role: "user" as const,
          content: "What metrics should I focus on?",
        },
        {
          role: "assistant" as const,
          content: "Focus on customer acquisition cost (CAC), lifetime value (LTV), market penetration rate, and customer retention. These will give you a clear picture of your expansion's health and sustainability.",
        },
      ],
    },
    {
      personaName: "Creative Writing Coach",
      title: "Story Development",
      messages: [
        {
          role: "user" as const,
          content: "I'm stuck on my character's motivation. Any tips?",
        },
        {
          role: "assistant" as const,
          content: "Great question! Every compelling character needs a core desire and an internal conflict. What does your character want most? And what's stopping them—internally—from achieving it? This tension drives authentic character development.",
        },
        {
          role: "user" as const,
          content: "She wants freedom but fears disappointing her family.",
        },
        {
          role: "assistant" as const,
          content: "Perfect! That's a rich emotional conflict. Now explore: What does freedom mean to her specifically? And what event will force her to choose between freedom and family approval? That choice is where your story finds its power.",
        },
      ],
    },
    {
      personaName: "Python Programming Mentor",
      title: "Code Optimization",
      messages: [
        {
          role: "user" as const,
          content: "My data processing script is really slow. How can I speed it up?",
        },
        {
          role: "assistant" as const,
          content: "Let's optimize! First, are you using pandas? Consider vectorized operations instead of loops. Second, if you're processing large files, use chunking. Third, implement multiprocessing for CPU-bound tasks. Share your code and I'll provide specific improvements.",
        },
      ],
    },
  ];

  return (
    <div className="min-h-screen bg-background">
      {/* Hero Section */}
      <MarketingHero />

      {/* Featured Personas by Category */}
      <section id="personas-section" className="py-20">
        <div className="container mx-auto px-4">
          <div className="max-w-6xl mx-auto">
            {/* Section Header */}
            <div className="text-center mb-16">
              <h2 className="text-3xl md:text-4xl font-bold mb-4">
                Explore AI Personas by Category
              </h2>
              <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
                Discover specialized AI personalities across every field. Each persona is trained to provide expert insights in their domain.
              </p>
            </div>

            {/* Category Cards Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {categoryGroupsWithPersonas.map((group) => (
                <PersonaCategoryCard
                  key={group.title}
                  title={group.title}
                  description={group.description}
                  icon={group.icon}
                  personaCount={group.count}
                  samplePersonas={group.personas.slice(0, 6).map((p) => ({
                    id: p.id,
                    displayName: p.displayName,
                    profileImageUrl: p.profileImageUrl,
                  }))}
                  gradient={group.gradient}
                />
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* Chat Preview Section */}
      <section className="py-20 bg-muted/30">
        <div className="container mx-auto px-4">
          <div className="max-w-6xl mx-auto">
            {/* Section Header */}
            <div className="text-center mb-16">
              <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-primary/10 border border-primary/20 mb-4">
                <MessageSquare className="h-4 w-4 text-primary" />
                <span className="text-sm font-medium">See It In Action</span>
              </div>
              <h2 className="text-3xl md:text-4xl font-bold mb-4">
                Real Conversations, Real Insights
              </h2>
              <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
                Experience natural, intelligent conversations with AI personas that understand context and provide valuable guidance.
              </p>
            </div>

            {/* Chat Examples Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {chatExamples.map((example, index) => (
                <ChatMockup
                  key={index}
                  personaName={example.personaName}
                  title={example.title}
                  messages={example.messages}
                />
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* How It Works Section */}
      <HowItWorks />

      {/* Final CTA Section */}
      <section className="py-20">
        <div className="container mx-auto px-4">
          <div className="max-w-4xl mx-auto">
            <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-primary/10 via-primary/5 to-background border-2 border-primary/20 p-12 text-center">
              {/* Background decoration */}
              <div className="absolute inset-0 bg-grid-white/5 bg-[size:20px_20px]" />
              
              <div className="relative space-y-6">
                <h2 className="text-3xl md:text-4xl font-bold">
                  Ready to Start Your Journey?
                </h2>
                <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
                  Join thousands discovering AI-powered conversations. Get personalized insights, expert advice, and creative inspiration—all tailored to your needs.
                </p>

                {/* Stats */}
                <div className="flex flex-wrap justify-center gap-8 py-6">
                  <div className="text-center">
                    <div className="text-3xl font-bold text-primary">
                      {totalPersonaCount}+
                    </div>
                    <div className="text-sm text-muted-foreground">
                      AI Personas
                    </div>
                  </div>
                  <div className="text-center">
                    <div className="text-3xl font-bold text-primary">
                      {totalChatCount > 1000 ? `${Math.floor(totalChatCount / 1000)}K+` : `${totalChatCount}+`}
                    </div>
                    <div className="text-sm text-muted-foreground">
                      Conversations
                    </div>
                  </div>
                  <div className="text-center">
                    <div className="text-3xl font-bold text-primary">
                      {categories.length}+
                    </div>
                    <div className="text-sm text-muted-foreground">
                      Categories
                    </div>
                  </div>
                </div>

                {/* CTA Button */}
                <div className="flex flex-col sm:flex-row gap-4 justify-center items-center pt-4">
                  <Button size="lg" asChild className="text-lg px-8 h-14">
                    <a href="/api/auth/signup">
                      Get Started Free
                      <ArrowRight className="ml-2 h-5 w-5" />
                    </a>
                  </Button>
                  <Button size="lg" variant="outline" asChild className="text-lg px-8 h-14">
                    <a href="/api/auth/login">
                      Sign In
                    </a>
                  </Button>
                </div>

                <p className="text-sm text-muted-foreground pt-4">
                  No credit card required. Start chatting in seconds.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

