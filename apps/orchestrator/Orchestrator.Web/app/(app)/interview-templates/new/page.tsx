"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Loader2, Save } from "lucide-react";
import { useServerAction } from "@/lib/use-server-action";
import {
  createInterviewTemplate,
  fetchAgentsForTemplate,
  fetchRolesForTemplate,
} from "../actions";

const DEFAULT_OPENING = "Hi {{applicantName}}, my name is {{agentName}} and I'll be conducting your interview today for the {{jobTitle}} position.";
const DEFAULT_CLOSING = "Thank you for completing the interview {{applicantName}}. Someone will be in touch soon regarding the {{jobTitle}} position.";

export default function NewInterviewTemplatePage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [roleTemplateId, setRoleTemplateId] = useState("");
  const [agentId, setAgentId] = useState("");
  const [openingTemplate, setOpeningTemplate] = useState(DEFAULT_OPENING);
  const [closingTemplate, setClosingTemplate] = useState(DEFAULT_CLOSING);

  const [agents, setAgents] = useState<{ id: string; displayName: string }[]>([]);
  const [roles, setRoles] = useState<{ id: string; roleKey: string; roleName: string; industry?: string | null }[]>([]);
  const [loadingOptions, setLoadingOptions] = useState(true);

  const { execute: executeCreate, isLoading: isCreating } = useServerAction(
    async () => {
      if (!name.trim()) throw new Error("Name is required");
      const created = await createInterviewTemplate({
        name: name.trim(),
        description: description.trim() || null,
        roleTemplateId: roleTemplateId || null,
        agentId: agentId || null,
        openingTemplate: openingTemplate.trim() || null,
        closingTemplate: closingTemplate.trim() || null,
      });
      router.push("/interview-templates");
    },
    { successMessage: "Interview template created!" }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user) {
      loadOptions();
    }
  }, [user]);

  const loadOptions = async () => {
    try {
      setLoadingOptions(true);
      const [agentList, roleList] = await Promise.all([
        fetchAgentsForTemplate(),
        fetchRolesForTemplate(),
      ]);
      setAgents(agentList);
      setRoles(roleList);
    } catch (err) {
      console.error("Failed to load options:", err);
    } finally {
      setLoadingOptions(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeCreate();
  };

  if (isUserLoading || loadingOptions) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) return null;

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="max-w-2xl mx-auto">
        <h1 className="text-3xl font-bold mb-6">New Interview Template</h1>

        <Card>
          <CardHeader>
            <CardTitle>Template Details</CardTitle>
            <CardDescription>
              Configure interview content, agent, and templates for this interview template
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-6">
              <div className="space-y-2">
                <Label htmlFor="name">
                  Name <span className="text-red-500">*</span>
                </Label>
                <Input
                  id="name"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder="e.g., Standard Phone Interview"
                  required
                  disabled={isCreating}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Description</Label>
                <Textarea
                  id="description"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Describe what this template is used for"
                  rows={2}
                  className="resize-none"
                  disabled={isCreating}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="roleTemplateId">Interview Content</Label>
                <select
                  id="roleTemplateId"
                  value={roleTemplateId}
                  onChange={(e) => setRoleTemplateId(e.target.value)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  disabled={isCreating}
                >
                  <option value="">Select interview content...</option>
                  {roles.map((role) => (
                    <option key={role.id} value={role.id}>
                      {role.roleName}{role.industry ? ` (${role.industry})` : ""}
                    </option>
                  ))}
                </select>
                <p className="text-sm text-muted-foreground">
                  All competencies from the selected content will be used automatically.
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="agentId">Agent</Label>
                <select
                  id="agentId"
                  value={agentId}
                  onChange={(e) => setAgentId(e.target.value)}
                  className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                  disabled={isCreating}
                >
                  <option value="">Select agent...</option>
                  {agents.map((agent) => (
                    <option key={agent.id} value={agent.id}>
                      {agent.displayName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <Label htmlFor="openingTemplate">Opening Template</Label>
                <Textarea
                  id="openingTemplate"
                  value={openingTemplate}
                  onChange={(e) => setOpeningTemplate(e.target.value)}
                  rows={3}
                  className="resize-none"
                  disabled={isCreating}
                />
                <p className="text-sm text-muted-foreground">
                  Available variables: {"{{applicantName}}"}, {"{{agentName}}"}, {"{{jobTitle}}"}
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="closingTemplate">Closing Template</Label>
                <Textarea
                  id="closingTemplate"
                  value={closingTemplate}
                  onChange={(e) => setClosingTemplate(e.target.value)}
                  rows={3}
                  className="resize-none"
                  disabled={isCreating}
                />
                <p className="text-sm text-muted-foreground">
                  Available variables: {"{{applicantName}}"}, {"{{agentName}}"}, {"{{jobTitle}}"}
                </p>
              </div>

              <div className="flex gap-3 pt-4">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => router.push("/interview-templates")}
                  disabled={isCreating}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={isCreating} className="flex-1">
                  {isCreating ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      Creating...
                    </>
                  ) : (
                    <>
                      <Save className="mr-2 h-4 w-4" />
                      Create Template
                    </>
                  )}
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
