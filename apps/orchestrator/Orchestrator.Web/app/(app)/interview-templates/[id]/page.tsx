"use client";

import { useEffect, useState } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useParams } from "next/navigation";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
import { Loader2, Save, Trash2, AlertTriangle } from "lucide-react";
import { useServerAction } from "@/lib/use-server-action";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  fetchInterviewTemplateById,
  updateInterviewTemplate,
  deleteInterviewTemplate,
  fetchAgentsForTemplate,
  fetchRolesForTemplate,
} from "../actions";

export default function EditInterviewTemplatePage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const params = useParams();
  const templateId = params.id as string;

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [roleTemplateId, setRoleTemplateId] = useState("");
  const [agentId, setAgentId] = useState("");
  const [openingTemplate, setOpeningTemplate] = useState("");
  const [closingTemplate, setClosingTemplate] = useState("");

  const [agents, setAgents] = useState<{ id: string; displayName: string }[]>([]);
  const [roles, setRoles] = useState<{ id: string; roleKey: string; roleName: string; industry?: string | null }[]>([]);

  const { execute: executeUpdate, isLoading: isSaving } = useServerAction(
    async () => {
      if (!name.trim()) throw new Error("Name is required");
      await updateInterviewTemplate(templateId, {
        name: name.trim(),
        description: description.trim() || null,
        roleTemplateId: roleTemplateId || null,
        agentId: agentId || null,
        openingTemplate: openingTemplate.trim() || null,
        closingTemplate: closingTemplate.trim() || null,
      });
    },
    { successMessage: "Template updated!" }
  );

  const { execute: executeDelete, isLoading: isDeleting } = useServerAction(
    () => deleteInterviewTemplate(templateId),
    {
      successMessage: "Template deleted!",
      onSuccess: () => router.push("/interview-templates"),
      onError: () => setDeleteDialogOpen(false),
    }
  );

  useEffect(() => {
    if (!isUserLoading && !user) {
      router.push("/api/auth/login");
    }
  }, [user, isUserLoading, router]);

  useEffect(() => {
    if (user && templateId) {
      loadData();
    }
  }, [user, templateId]);

  const loadData = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const [tpl, agentList, roleList] = await Promise.all([
        fetchInterviewTemplateById(templateId),
        fetchAgentsForTemplate(),
        fetchRolesForTemplate(),
      ]);
      setName(tpl.name);
      setDescription(tpl.description || "");
      setRoleTemplateId(tpl.roleTemplateId || "");
      setAgentId(tpl.agentId || "");
      setOpeningTemplate(tpl.openingTemplate || "");
      setClosingTemplate(tpl.closingTemplate || "");
      setAgents(agentList);
      setRoles(roleList);
    } catch (err) {
      console.error("Error loading template:", err);
      setError("Failed to load template.");
    } finally {
      setIsLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await executeUpdate();
  };

  if (isUserLoading || isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  if (!user) return null;

  return (
    <>
      <div className="container mx-auto px-4 py-8">
        <div className="max-w-2xl mx-auto space-y-6">
          {error && (
            <div className="p-4 bg-destructive/10 text-destructive rounded-lg border border-destructive/20">
              {error}
            </div>
          )}

          <Card>
            <CardHeader>
              <CardTitle>Edit Interview Template</CardTitle>
              <CardDescription>Update template settings</CardDescription>
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
                    required
                    disabled={isSaving}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="description">Description</Label>
                  <Textarea
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    rows={2}
                    className="resize-none"
                    disabled={isSaving}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="roleTemplateId">Interview Content</Label>
                  <select
                    id="roleTemplateId"
                    value={roleTemplateId}
                    onChange={(e) => setRoleTemplateId(e.target.value)}
                    className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                    disabled={isSaving}
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
                    disabled={isSaving}
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
                    disabled={isSaving}
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
                    disabled={isSaving}
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
                    disabled={isSaving}
                  >
                    Cancel
                  </Button>
                  <Button type="submit" disabled={isSaving} className="flex-1">
                    {isSaving ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        Saving...
                      </>
                    ) : (
                      <>
                        <Save className="mr-2 h-4 w-4" />
                        Save Changes
                      </>
                    )}
                  </Button>
                </div>
              </form>
            </CardContent>
          </Card>

          <Card className="border-destructive">
            <CardHeader>
              <div className="flex items-center gap-2">
                <AlertTriangle className="h-5 w-5 text-destructive" />
                <CardTitle className="text-destructive">Danger Zone</CardTitle>
              </div>
            </CardHeader>
            <CardContent>
              <div className="flex items-center justify-between p-4 border border-destructive/20 rounded-lg bg-destructive/5">
                <div>
                  <h4 className="font-semibold text-sm">Delete this template</h4>
                  <p className="text-sm text-muted-foreground mt-1">
                    This action cannot be undone.
                  </p>
                </div>
                <Button
                  variant="destructive"
                  onClick={() => setDeleteDialogOpen(true)}
                  disabled={isDeleting}
                >
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete
                </Button>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Are you absolutely sure?</AlertDialogTitle>
            <AlertDialogDescription>
              This will permanently delete <strong>{name}</strong>. This action cannot be undone.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isDeleting}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => executeDelete()}
              disabled={isDeleting}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {isDeleting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Deleting...
                </>
              ) : (
                <>
                  <Trash2 className="mr-2 h-4 w-4" />
                  Delete Template
                </>
              )}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
